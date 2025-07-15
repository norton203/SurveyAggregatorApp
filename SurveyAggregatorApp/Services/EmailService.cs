// Services/EmailService.cs - Email notification service
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SurveyAggregatorApp.Models;

namespace SurveyAggregatorApp.Services
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Survey Hub";
        public bool EnableSsl { get; set; } = true;
    }

    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to Survey Hub!";
            var body = $@"
                <h2>Welcome to Survey Hub, {userName}!</h2>
                <p>Thank you for joining Survey Hub. You're now ready to start earning money from surveys!</p>
                
                <h3>Next Steps:</h3>
                <ol>
                    <li><strong>Complete your profile</strong> - This helps us match you with relevant surveys</li>
                    <li><strong>Connect survey accounts</strong> - Link your existing accounts from Pollfish, Dynata, and more</li>
                    <li><strong>Start earning</strong> - Begin taking surveys and track your earnings in one place</li>
                </ol>
                
                <p><a href='https://yourapp.com/profile' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Complete Your Profile</a></p>
                
                <p>If you have any questions, feel free to contact our support team.</p>
                
                <p>Happy earning!<br>The Survey Hub Team</p>
            ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendSurveyNotificationAsync(string toEmail, string userName, List<ExternalSurvey> newSurveys)
        {
            if (!newSurveys.Any()) return false;

            var subject = $"New surveys available - Earn up to £{newSurveys.Max(s => s.Reward):F2}!";
            var surveyList = string.Join("", newSurveys.Take(5).Select(s =>
                $"<li><strong>{s.Title}</strong> - £{s.Reward:F2} ({s.EstimatedMinutes} min)</li>"));

            var body = $@"
                <h2>New Surveys Available, {userName}!</h2>
                <p>We've found {newSurveys.Count} new surveys that match your profile:</p>
                
                <ul>{surveyList}</ul>
                
                {(newSurveys.Count > 5 ? $"<p>...and {newSurveys.Count - 5} more!</p>" : "")}
                
                <p><a href='https://yourapp.com/surveys' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View All Surveys</a></p>
                
                <p>Don't miss out on these earning opportunities!</p>
                
                <p>Best regards,<br>The Survey Hub Team</p>
            ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEarningsUpdateAsync(string toEmail, string userName, decimal amount, string surveyTitle)
        {
            var subject = $"Survey completed - £{amount:F2} earned!";
            var body = $@"
                <h2>Congratulations, {userName}!</h2>
                <p>You've successfully completed the survey: <strong>{surveyTitle}</strong></p>
                
                <div style='background-color: #d4edda; border: 1px solid #c3e6cb; border-radius: 5px; padding: 15px; margin: 20px 0;'>
                    <h3 style='color: #155724; margin: 0;'>Earnings: £{amount:F2}</h3>
                </div>
                
                <p><a href='https://yourapp.com/earnings' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View Earnings Dashboard</a></p>
                
                <p>Keep up the great work!</p>
                
                <p>Best regards,<br>The Survey Hub Team</p>
            ";

            return await SendEmailAsync(toEmail, subject, body);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);

                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }
    }
}
