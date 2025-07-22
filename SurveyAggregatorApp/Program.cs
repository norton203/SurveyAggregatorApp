using Microsoft.EntityFrameworkCore;
using SurveyAggregatorApp.Models;
using SurveyAggregatorApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
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
    });

builder.Services.AddAuthorization();

// Add HTTP Client
builder.Services.AddHttpClient();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SurveyProviderService>();
builder.Services.AddHttpContextAccessor(); // Add this line
builder.Services.AddScoped<SurveyAggregatorApp.Services.AuthenticationService>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddSingleton<StateContainer>();

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();