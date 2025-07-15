// Enhanced ProviderAccount model
using SurveyAggregatorApp.Models;
using System.ComponentModel.DataAnnotations;

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
    public DateTime? LastSyncAt { get; set; }
    public decimal EarningsFromProvider { get; set; } = 0;
    public int SurveysCompleted { get; set; } = 0;

    // Navigation property
    public User User { get; set; } = null!;
}