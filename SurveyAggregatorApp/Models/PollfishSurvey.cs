using System.Text.Json.Serialization;

public class PollfishSurvey
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("reward_cents")]
    public int Reward { get; set; } // In cents

    [JsonPropertyName("estimated_duration")]
    public int EstimatedDuration { get; set; } // In seconds

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("survey_url")]
    public string SurveyUrl { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("completion_rate")]
    public decimal CompletionRate { get; set; }

    [JsonPropertyName("targeting")]
    public Dictionary<string, object> Targeting { get; set; } = new();
}
