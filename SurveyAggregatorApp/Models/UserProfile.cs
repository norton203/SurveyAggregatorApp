// Enhanced user profile model
using SurveyAggregatorApp.Models;
using System.ComponentModel.DataAnnotations;

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
    public string? Gender { get; set; } // Male, Female, Other, PreferNotToSay
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