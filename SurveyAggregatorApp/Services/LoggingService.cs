// Services/LoggingService.cs - Application logging service
using SurveyAggregatorApp.Models;

namespace SurveyAggregatorApp.Services
{
    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly ApplicationDbContext _context;

        public LoggingService(ILogger<LoggingService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task LogUserActionAsync(int userId, string action, string details = "", string ipAddress = "")
        {
            try
            {
                _logger.LogInformation($"User {userId}: {action} - {details}");

                // Could also save to database for audit trail
                var logEntry = new UserActivityLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                // _context.UserActivityLogs.Add(logEntry);
                // await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log user action: {action}");
            }
        }

        public async Task LogSurveyCompletionAsync(int userId, string surveyId, string providerId, decimal reward)
        {
            await LogUserActionAsync(userId, "SurveyCompleted", $"Survey: {surveyId}, Provider: {providerId}, Reward: £{reward:F2}");
        }

        public async Task LogProviderConnectionAsync(int userId, string providerId, bool success)
        {
            var action = success ? "ProviderConnected" : "ProviderConnectionFailed";
            await LogUserActionAsync(userId, action, $"Provider: {providerId}");
        }

        public async Task LogErrorAsync(string component, string error, Exception? exception = null)
        {
            _logger.LogError(exception, $"{component}: {error}");
        }
    }

    // Optional: Database model for user activity logs
    public class UserActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}