using System.Text.Json.Serialization;

namespace SurveyAggregatorApp.Services
{
    namespace SurveyAggregatorApp.Services
    {
        using global::SurveyAggregatorApp.Models;
        using Microsoft.Extensions.Options;
        using System.Text.Json;
        using System.Text.Json.Serialization;

        public class SurveyProviderService
        {
            private readonly HttpClient _httpClient;
            private readonly List<SurveyProvider> _providers;
            private readonly PollfishApiService _pollfishService;

            public SurveyProviderService(HttpClient httpClient, IOptions<SurveyProviderOptions> options)
            {
                _httpClient = httpClient;
                _providers = InitializeProviders();
                _pollfishService = new PollfishApiService(_httpClient);
            }

            private List<SurveyProvider> InitializeProviders()
            {
                return new List<SurveyProvider>
            {
                new()
                {
                    Id = "pollfish",
                    Name = "Pollfish",
                    LogoUrl = "/images/pollfish-logo.png",
                    Description = "Real-time survey platform with instant rewards",
                    ApiEndpoint = "https://api.pollfish.com/v2/",
                    AuthUrl = "https://www.pollfish.com/oauth/authorize",
                    MinPayout = 0.30m,
                    PaymentMethods = new() { "PayPal", "Bank Transfer", "Gift Cards" }
                },
                new()
                {
                    Id = "dynata",
                    Name = "Dynata",
                    LogoUrl = "/images/dynata-logo.png",
                    Description = "Leading market research and data platform",
                    ApiEndpoint = "https://api.dynata.com/v1/",
                    AuthUrl = "https://portal.dynata.com/oauth/authorize",
                    MinPayout = 0.50m,
                    PaymentMethods = new() { "PayPal", "Bank Transfer" }
                },
                new()
                {
                    Id = "lucid",
                    Name = "Lucid (Cint)",
                    LogoUrl = "/images/lucid-logo.png",
                    Description = "Sample marketplace for market research",
                    ApiEndpoint = "https://api.luc.id/v1/",
                    AuthUrl = "https://suppliers.luc.id/oauth/authorize",
                    MinPayout = 0.25m,
                    PaymentMethods = new() { "PayPal", "Wire Transfer" }
                },
                new()
                {
                    Id = "surveymonkey",
                    Name = "SurveyMonkey Audience",
                    LogoUrl = "/images/surveymonkey-logo.png",
                    Description = "Survey creation and audience platform",
                    ApiEndpoint = "https://api.surveymonkey.com/v3/",
                    AuthUrl = "https://api.surveymonkey.com/oauth/authorize",
                    MinPayout = 1.00m,
                    PaymentMethods = new() { "PayPal", "Gift Cards" }
                }
            };
            }

        }

        // Real Pollfish API Service Implementation
        public class PollfishApiService
        {
            private readonly HttpClient _httpClient;
            private readonly string _baseUrl = "https://api.pollfish.com/v2/";
            private readonly string _apiKey; // This should come from configuration

            public PollfishApiService(HttpClient httpClient)
            {
                _httpClient = httpClient;
                // In production, get this from configuration/environment variables
                _apiKey = Environment.GetEnvironmentVariable("POLLFISH_API_KEY") ?? "demo_api_key";
            }

            public async Task<List<ExternalSurvey>> GetAvailableSurveysAsync(User user, string userToken)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}surveys/available");
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                    request.Headers.Add("User-Agent", "SurveyHub/1.0");

                    // Add user demographic filters
                    var queryParams = new List<string>
                {
                    $"country={GetUserCountry(user)}",
                    $"age_min=18",
                    $"age_max=65",
                    $"limit=20"
                };

                    request.RequestUri = new Uri($"{_baseUrl}surveys/available?{string.Join("&", queryParams)}");

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var pollfishResponse = JsonSerializer.Deserialize<PollfishSurveysResponse>(content);

                        return pollfishResponse.Surveys.Select(ConvertToExternalSurvey).ToList();
                    }
                    else
                    {
                        // Handle API errors gracefully
                        Console.WriteLine($"Pollfish API error: {response.StatusCode}");
                        return new List<ExternalSurvey>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling Pollfish API: {ex.Message}");
                    return new List<ExternalSurvey>();
                }
            }

            public async Task<string> GetSurveyLinkAsync(string surveyId, User user, string userToken)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}surveys/{surveyId}/link");
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");

                    var requestBody = new
                    {
                        respondent_id = user.Id.ToString(),
                        custom_answers = new
                        {
                            age = CalculateAge(user.JoinedDate), // Estimate from joined date
                            gender = "prefer_not_to_say", // Would need user profile
                            country = GetUserCountry(user)
                        },
                        callback_url = $"https://yourapp.com/api/pollfish/complete/{user.Id}/{surveyId}"
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var linkResponse = JsonSerializer.Deserialize<PollfishLinkResponse>(content);
                        return linkResponse.SurveyUrl;
                    }

                    return string.Empty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting Pollfish survey link: {ex.Message}");
                    return string.Empty;
                }
            }

            public async Task<bool> ValidateCompletionAsync(string surveyId, string respondentId, string completionToken)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}surveys/{surveyId}/validate");
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");

                    var requestBody = new
                    {
                        respondent_id = respondentId,
                        completion_token = completionToken
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error validating Pollfish completion: {ex.Message}");
                    return false;
                }
            }

            private ExternalSurvey ConvertToExternalSurvey(PollfishSurvey pollfishSurvey)
            {
                return new ExternalSurvey
                {
                    Id = $"pollfish_{pollfishSurvey.Id}",
                    ProviderId = "pollfish",
                    ProviderName = "Pollfish",
                    Title = pollfishSurvey.Title,
                    Description = pollfishSurvey.Description ?? "Complete this survey to earn rewards",
                    Reward = pollfishSurvey.Reward / 100m, // Pollfish returns cents
                    EstimatedMinutes = pollfishSurvey.EstimatedDuration / 60, // Convert seconds to minutes
                    Category = pollfishSurvey.Category ?? "General",
                    SurveyUrl = pollfishSurvey.SurveyUrl,
                    IsAvailable = pollfishSurvey.IsActive,
                    Metadata = new Dictionary<string, object>
                    {
                        ["pollfish_id"] = pollfishSurvey.Id,
                        ["completion_rate"] = pollfishSurvey.CompletionRate,
                        ["targeting"] = pollfishSurvey.Targeting
                    }
                };
            }

            private string GetUserCountry(User user)
            {
                // In a real app, you'd get this from user profile
                // For now, default to UK since you mentioned Glastonbury
                return "GB";
            }

            private int CalculateAge(DateTime joinedDate)
            {
                // Rough estimate - in real app you'd have actual birth date
                // Default to mid-range for surveys
                return 30;
            }
        }

       

       
       

        public async Task<List<SurveyProvider>> GetProvidersAsync()
            {
                await Task.Delay(100);
                return _providers.Where(p => p.IsActive).ToList();
            }

            public async Task<List<ExternalSurvey>> GetAvailableSurveysAsync(User user)
            {
                var allSurveys = new List<ExternalSurvey>();

                var connectedProviders = user.ConnectedAccounts
                    .Where(a => a.IsConnected)
                    .ToList();

                var completedSurveyIds = user.CompletedSurveys.Select(s => s.SurveyId).ToList();

                // Get surveys from Pollfish if connected
                var pollfishAccount = connectedProviders.FirstOrDefault(a => a.ProviderId == "pollfish");
                if (pollfishAccount != null)
                {
                    try
                    {
                        var pollfishSurveys = await _pollfishService.GetAvailableSurveysAsync(user, pollfishAccount.UserToken);
                        allSurveys.AddRange(pollfishSurveys.Where(s => !completedSurveyIds.Contains(s.Id)));
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail completely
                        Console.WriteLine($"Error fetching Pollfish surveys: {ex.Message}");
                    }
                }

                // Add other providers here as they're implemented
                // var dynataAccount = connectedProviders.FirstOrDefault(a => a.ProviderId == "dynata");
                // if (dynataAccount != null) { ... }

                return allSurveys;
            }

            public async Task<List<ExternalSurvey>> GetSurveysByProviderAsync(string providerId, string userToken)
            {
                await Task.Delay(300);

                if (providerId == "pollfish")
                {
                    // This would need a user context - for now return empty
                    // In practice, this method might not be needed with the new architecture
                    return new List<ExternalSurvey>();
                }

                // Other providers would be handled here
                return new List<ExternalSurvey>();
            }

            public async Task<bool> AuthorizeProviderAsync(string providerId, string authCode)
            {
                await Task.Delay(500);

                if (providerId == "pollfish")
                {
                    // Pollfish uses API keys rather than OAuth in some cases
                    // This would validate the API key or OAuth token
                    return !string.IsNullOrEmpty(authCode);
                }

                // Other providers would have OAuth flows here
                return true; // Mock success for demo
            }

            public async Task<string> GetProviderAuthUrlAsync(string providerId, string redirectUri)
            {
                await Task.Delay(100);

                var provider = _providers.FirstOrDefault(p => p.Id == providerId);
                if (provider != null)
                {
                    if (providerId == "pollfish")
                    {
                        // Pollfish might use direct API key setup rather than OAuth
                        return $"https://www.pollfish.com/publisher/api-setup?redirect_uri={redirectUri}";
                    }

                    // Standard OAuth URL for other providers
                    return $"{provider.AuthUrl}?client_id=your_client_id&redirect_uri={redirectUri}&response_type=code&scope=surveys";
                }

                return string.Empty;
            }

            public SurveyProvider? GetProvider(string providerId)
            {
                return _providers.FirstOrDefault(p => p.Id == providerId);
            }

            // Handle webhook notifications from Pollfish
            public async Task<bool> HandlePollfishWebhookAsync(PollfishWebhookPayload payload)
            {
                try
                {
                    // Validate webhook signature (important for security)
                    if (!await ValidatePollfishWebhookAsync(payload))
                    {
                        return false;
                    }

                    // Process the completion
                    if (payload.EventType == "survey_completed")
                    {
                        // Update user earnings
                        // This would integrate with your UserService
                        Console.WriteLine($"Survey {payload.SurveyId} completed by user {payload.RespondentId}");
                        Console.WriteLine($"Reward: £{payload.RewardCents / 100m:F2}");

                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing Pollfish webhook: {ex.Message}");
                    return false;
                }
            }

            private async Task<bool> ValidatePollfishWebhookAsync(PollfishWebhookPayload payload)
            {
                // In production, validate the webhook signature using Pollfish's secret
                // This prevents malicious requests
                await Task.Delay(50);
                return true; // Mock validation for demo
            }

            // Generic webhook handler for survey completion notifications
            public async Task<bool> HandleSurveyCompletionWebhook(string providerId, string surveyId, string externalUserId, decimal reward)
            {
                await Task.Delay(100);

                Console.WriteLine($"Survey completion from {providerId}: Survey {surveyId}, User {externalUserId}, Reward £{reward:F2}");

                return true;
            }
        }
    }
   
}

