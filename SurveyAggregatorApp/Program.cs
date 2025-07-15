using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using SurveyAggregatorApp.Components;
using SurveyAggregatorApp.Models;
using SurveyAggregatorApp.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=surveyapp.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);

    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

// HTTP Context and Client Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Memory Caching
builder.Services.AddMemoryCache();

// Response Compression
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Application Services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SurveyProviderService>();
builder.Services.AddScoped<StateContainer>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddScoped<EmailService>();

// Background Services
builder.Services.AddHostedService<BackgroundSurveyService>();

// Configuration Options
builder.Services.Configure<SurveyProviderOptions>(
    builder.Configuration.GetSection("SurveyProviders"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("memory", () =>
    {
        var allocatedBytes = GC.GetTotalMemory(false);
        var maxBytes = 1024L * 1024L * 1024L; // 1 GB limit
        return allocatedBytes < maxBytes
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy();
    });

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
        }
        else
        {
            context.Database.EnsureCreated();
        }

        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        throw; // Fail fast on database issues
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseResponseCompression(); // Only in production for better performance
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security Middleware
app.UseMiddleware<SecurityMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Health Check Endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API Endpoints
app.MapPost("/api/pollfish/webhook", async (
    PollfishWebhookPayload payload,
    SurveyProviderService surveyService,
    UserService userService,
    SecurityService securityService,
    ApplicationDbContext context,
    ILogger<Program> logger) =>
{
    try
    {
        // Validate webhook signature (implement based on Pollfish documentation)
        var signature = context.Request.Headers["X-Pollfish-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Webhook received without signature");
            return Results.Unauthorized();
        }

        var success = await surveyService.HandlePollfishWebhookAsync(payload);
        if (success && payload.EventType == "survey_completed")
        {
            var user = await context.Users
                .Include(u => u.ConnectedAccounts)
                .Where(u => u.ConnectedAccounts.Any(ca =>
                    ca.ProviderId == "pollfish" &&
                    ca.ExternalUserId == payload.RespondentId))
                .FirstOrDefaultAsync();

            if (user != null)
            {
                var reward = payload.RewardCents / 100m;

                var externalSurvey = new ExternalSurvey
                {
                    Id = $"pollfish_{payload.SurveyId}",
                    ProviderId = "pollfish",
                    ProviderName = "Pollfish",
                    Title = "Pollfish Survey",
                    Reward = reward,
                    EstimatedMinutes = 5
                };

                await userService.RecordSurveyCompletionAsync(user.Id, externalSurvey);
                logger.LogInformation($"Recorded survey completion for user {user.Id}: £{reward:F2}");
            }
        }
        return success ? Results.Ok() : Results.BadRequest();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Webhook processing error");
        return Results.StatusCode(500);
    }
});

// Environment information
var environmentInfo = new
{
    Environment = app.Environment.EnvironmentName,
    Database = connectionString.Contains("Data Source=") ? "SQLite" : "Unknown",
    Version = "1.0.0",
    StartTime = DateTime.UtcNow
};

app.Logger.LogInformation("Survey Hub starting: {@EnvironmentInfo}", environmentInfo);

app.Run();
