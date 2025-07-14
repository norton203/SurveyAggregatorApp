// Program.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SurveyAggregatorApp.Data;
using SurveyAggregatorApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SurveyProviderService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<StateContainer>();

// Add configuration for survey providers
builder.Services.Configure<SurveyProviderOptions>(
    builder.Configuration.GetSection("SurveyProviders"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Add API endpoints for webhooks
app.MapPost("/api/pollfish/webhook", async (PollfishWebhookPayload payload, SurveyProviderService surveyService, UserService userService) =>
{
    try
    {
        var success = await surveyService.HandlePollfishWebhookAsync(payload);
        if (success && payload.EventType == "survey_completed")
        {
            // Update user earnings
            if (int.TryParse(payload.RespondentId, out int userId))
            {
                var reward = payload.RewardCents / 100m;
                await userService.UpdateUserBalanceAsync(userId, reward);
                await userService.MarkSurveyCompletedAsync(userId, int.Parse(payload.SurveyId.Replace("pollfish_", "")));
            }
        }
        return success ? Results.Ok() : Results.BadRequest();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Webhook error: {ex.Message}");
        return Results.StatusCode(500);
    }
});

app.Run();
