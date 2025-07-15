// Enhanced CompletedSurvey model
using SurveyAggregatorApp.Models;
using System.ComponentModel.DataAnnotations;

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
    public string Status { get; set; } = "Completed"; // Completed, Pending, Rejected, Disputed
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
