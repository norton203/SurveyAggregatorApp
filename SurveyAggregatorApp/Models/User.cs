// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace SurveyAggregatorApp.Models
{
    // Updated User model for production
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public decimal TotalEarnings { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public UserProfile? Profile { get; set; }
        public ICollection<ProviderAccount> ConnectedAccounts { get; set; } = new List<ProviderAccount>();
        public ICollection<CompletedSurvey> CompletedSurveys { get; set; } = new List<CompletedSurvey>();
        public ICollection<UserTransaction> Transactions { get; set; } = new List<UserTransaction>();
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
