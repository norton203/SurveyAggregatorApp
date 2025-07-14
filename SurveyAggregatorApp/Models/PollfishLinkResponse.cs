using System.Text.Json.Serialization;

public class PollfishLinkResponse
{
    [JsonPropertyName("survey_url")]
    public string SurveyUrl { get; set; } = string.Empty;

    [JsonPropertyName("respondent_id")]
    public string RespondentId { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
}