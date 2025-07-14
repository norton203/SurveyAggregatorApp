// 6. Fix Models/PollfishSurveyResponse.cs - REMOVE BAD USING STATEMENT
using System.Text.Json.Serialization;

public class PollfishSurveysResponse
{
    [JsonPropertyName("surveys")]
    public List<PollfishSurvey> Surveys { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}