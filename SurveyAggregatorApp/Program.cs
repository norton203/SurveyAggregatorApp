using Microsoft.EntityFrameworkCore;
using SurveyAggregatorApp.Models;
using SurveyAggregatorApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using SurveyAggregatorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Data Source=surveyaggregator.db"));

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Add HTTP Client and Memory Cache
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Add HttpContextAccessor BEFORE other services that depend on it
builder.Services.AddHttpContextAccessor();

// Register application services in correct order
builder.Services.AddSingleton<SecurityService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SurveyProviderService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddSingleton<StateContainer>();

// Add configuration for EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Add hosted services
builder.Services.AddHostedService<BackgroundSurveyService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Add security middleware
app.UseMiddleware<SecurityMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure database is created and seed demo data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    try
    {
        context.Database.EnsureCreated();
        await userService.EnsureDemoDataAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();