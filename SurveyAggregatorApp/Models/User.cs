// Models/User.cs
namespace SurveyAggregatorApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal TotalEarnings { get; set; } = 0;
        public DateTime JoinedDate { get; set; } = DateTime.Now;
        public List<ProviderAccount> ConnectedAccounts { get; set; } = new();
        public List<CompletedSurvey> CompletedSurveys { get; set; } = new();
    }

    public class ProviderAccount
    {
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string UserToken { get; set; } = string.Empty;
        public string ExternalUserId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime ConnectedDate { get; set; } = DateTime.Now;
        public decimal EarningsFromProvider { get; set; } = 0;
    }

    public class CompletedSurvey
    {
        public string SurveyId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public DateTime CompletedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Completed"; // Completed, Pending, Rejected
    }
}
