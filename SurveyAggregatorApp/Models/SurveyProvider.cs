// Models/SurveyProvider.cs
namespace SurveyAggregatorApp.Models
{
    public class SurveyProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal MinPayout { get; set; }
        public List<string> PaymentMethods { get; set; } = new();
    }

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
