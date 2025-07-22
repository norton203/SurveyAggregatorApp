using System.ComponentModel.DataAnnotations;

namespace SurveyAggregatorApp.Models
{
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
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow; // For compatibility

        // Computed property for compatibility
        public string Name => Profile?.FullName ?? Email;

        // Navigation properties
        public UserProfile? Profile { get; set; }
        public ICollection<ProviderAccount> ConnectedAccounts { get; set; } = new List<ProviderAccount>();
        public ICollection<CompletedSurvey> CompletedSurveys { get; set; } = new List<CompletedSurvey>();
        public ICollection<UserTransaction> Transactions { get; set; } = new List<UserTransaction>();
    }

    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Country { get; set; } = "GB";

        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Address { get; set; }

        // Demographics for survey targeting
        public string? Gender { get; set; }
        public string? EducationLevel { get; set; }
        public string? EmploymentStatus { get; set; }
        public decimal? AnnualIncome { get; set; }
        public string? Industry { get; set; }

        // Preferences
        public bool ReceiveEmailNotifications { get; set; } = true;
        public bool ReceiveSmsNotifications { get; set; } = false;
        public string? PreferredLanguage { get; set; } = "en";
        public string TimeZone { get; set; } = "GMT";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;

        // Computed properties
        public string FullName => $"{FirstName} {LastName}";
        public int? Age => DateOfBirth?.Date != null
            ? (int)((DateTime.Now - DateOfBirth.Value.Date).TotalDays / 365.25)
            : null;
    }

    public class ProviderAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string ProviderId { get; set; } = string.Empty;

        [Required]
        public string ProviderName { get; set; } = string.Empty;

        public string UserToken { get; set; } = string.Empty;
        public string ExternalUserId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime ConnectedDate { get; set; } = DateTime.UtcNow; // For compatibility
        public DateTime? LastSyncAt { get; set; }
        public decimal EarningsFromProvider { get; set; } = 0;
        public int SurveysCompleted { get; set; } = 0;

        // Navigation property
        public User User { get; set; } = null!;
    }

    public class CompletedSurvey
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string SurveyId { get; set; } = string.Empty;

        [Required]
        public string ProviderId { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public decimal Reward { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public DateTime CompletedDate { get; set; } = DateTime.UtcNow; // For compatibility
        public string Status { get; set; } = "Completed";
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }

    public class UserTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public decimal Amount { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string Status { get; set; } = "Completed";
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}