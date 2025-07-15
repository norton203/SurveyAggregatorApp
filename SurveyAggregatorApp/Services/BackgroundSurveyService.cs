// Services/BackgroundSurveyService.cs - Background service for survey updates
using SurveyAggregatorApp.Models;

namespace SurveyAggregatorApp.Services
{
    public class BackgroundSurveyService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundSurveyService> _logger;

        public BackgroundSurveyService(IServiceProvider serviceProvider, ILogger<BackgroundSurveyService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateSurveysAsync();
                    await NotifyUsersOfNewSurveysAsync();

                    // Wait 30 minutes before next update
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background survey service");
                    // Wait 5 minutes before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task UpdateSurveysAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var surveyService = scope.ServiceProvider.GetRequiredService<SurveyProviderService>();

            _logger.LogInformation("Starting survey update process");

            // Get users with connected accounts
            var usersWithConnections = await context.Users
                .Include(u => u.ConnectedAccounts)
                .Where(u => u.IsActive && u.ConnectedAccounts.Any(ca => ca.IsConnected))
                .ToListAsync();

            foreach (var user in usersWithConnections)
            {
                try
                {
                    var surveys = await surveyService.GetAvailableSurveysAsync(user);
                    _logger.LogInformation($"Found {surveys.Count} surveys for user {user.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating surveys for user {user.Id}");
                }
            }
        }

        private async Task NotifyUsersOfNewSurveysAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // This is a simplified version - in production, you'd track which surveys
            // have been notified about and only send notifications for truly new ones

            var usersToNotify = await context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Where(u => u.IsActive &&
                           u.Profile != null &&
                           u.Profile.ReceiveEmailNotifications &&
                           u.ConnectedAccounts.Any(ca => ca.IsConnected))
                .ToListAsync();

            foreach (var user in usersToNotify)
            {
                try
                {
                    // In a real implementation, you'd check for new surveys since last notification
                    // await emailService.SendSurveyNotificationAsync(user.Email, user.Profile.FullName, newSurveys);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending notification to user {user.Id}");
                }
            }
        }
    }
}