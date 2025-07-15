// New UserTransaction model for financial tracking
using SurveyAggregatorApp.Models;
using System.ComponentModel.DataAnnotations;

public class UserTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty; // Earning, Withdrawal, Bonus, Penalty

    public string? Description { get; set; }
    public string Status { get; set; } = "Completed"; // Pending, Completed, Failed, Cancelled
    public string? Reference { get; set; } // External transaction reference
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
