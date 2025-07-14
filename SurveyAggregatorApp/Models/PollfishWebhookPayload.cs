// Pollfish Webhook Models
using System.Text.Json.Serialization;

public class PollfishWebhookPayload
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("survey_id")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonPropertyName("respondent_id")]
    public string RespondentId { get; set; } = string.Empty;

    [JsonPropertyName("reward_cents")]
    public int RewardCents { get; set; }

    [JsonPropertyName("completion_time")]
    public DateTime CompletionTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // completed, terminated, quota_full

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}