using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SurveyAggregatorApp.Models
{
    public class SurveyProvider
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string LogoUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal MinPayout { get; set; }

        // Store as JSON string in database
        public string PaymentMethods { get; set; } = string.Empty;

        // Helper property for easy access
        [NotMapped]
        public List<string> PaymentMethodsList
        {
            get
            {
                if (string.IsNullOrEmpty(PaymentMethods))
                    return new List<string>();

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(PaymentMethods) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                PaymentMethods = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ExternalSurvey class - NOW PROPERLY NAMESPACED
    public class ExternalSurvey
    {
        public string Id { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public int EstimatedMinutes { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SurveyUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SurveyProviderOptions
    {
        public List<SurveyProvider> Providers { get; set; } = new();
    }
}