// 4. Fix Services/UserService.cs - ADD MISSING METHODS
// Services/UserService.cs
using SurveyAggregatorApp.Models;

namespace SurveyAggregatorApp.Services
{
    public class UserService
    {
        private readonly List<User> _users = new();
        private int _nextId = 1;

        public UserService()
        {
            // Add sample user with Pollfish connection
            var user = new User
            {
                Id = _nextId++,
                Email = "user@example.com",
                Name = "John Doe",
                TotalEarnings = 15.75m
            };

            user.ConnectedAccounts.AddRange(new[]
            {
                new ProviderAccount
                {
                    ProviderId = "pollfish",
                    ProviderName = "Pollfish",
                    IsConnected = true,
                    EarningsFromProvider = 8.50m,
                    UserToken = "pollfish_demo_token_123",
                    ConnectedDate = DateTime.Now.AddDays(-7)
                },
                new ProviderAccount
                {
                    ProviderId = "dynata",
                    ProviderName = "Dynata",
                    IsConnected = false,
                    EarningsFromProvider = 0m,
                    UserToken = string.Empty
                },
                new ProviderAccount
                {
                    ProviderId = "lucid",
                    ProviderName = "Lucid (Cint)",
                    IsConnected = false,
                    EarningsFromProvider = 0m,
                    UserToken = string.Empty
                }
            });

            user.CompletedSurveys.AddRange(new[]
            {
                new CompletedSurvey
                {
                    SurveyId = "pollfish_001",
                    ProviderId = "pollfish",
                    Title = "Consumer Preferences Study",
                    Reward = 1.50m,
                    CompletedDate = DateTime.Now.AddDays(-1),
                    Status = "Completed"
                },
                new CompletedSurvey
                {
                    SurveyId = "pollfish_002",
                    ProviderId = "pollfish",
                    Title = "Brand Awareness Survey",
                    Reward = 2.25m,
                    CompletedDate = DateTime.Now.AddDays(-3),
                    Status = "Completed"
                },
                new CompletedSurvey
                {
                    SurveyId = "pollfish_003",
                    ProviderId = "pollfish",
                    Title = "Product Feedback Collection",
                    Reward = 4.75m,
                    CompletedDate = DateTime.Now.AddDays(-5),
                    Status = "Completed"
                }
            });

            _users.Add(user);
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            await Task.Delay(100);
            return _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<User> RegisterAsync(string email, string name)
        {
            await Task.Delay(100);
            var user = new User
            {
                Id = _nextId++,
                Email = email,
                Name = name
            };
            _users.Add(user);
            return user;
        }

        public async Task<bool> ConnectProviderAsync(int userId, string providerId, string token)
        {
            await Task.Delay(200);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                var existingAccount = user.ConnectedAccounts.FirstOrDefault(a => a.ProviderId == providerId);
                if (existingAccount != null)
                {
                    existingAccount.IsConnected = true;
                    existingAccount.UserToken = token;
                }
                else
                {
                    user.ConnectedAccounts.Add(new ProviderAccount
                    {
                        ProviderId = providerId,
                        ProviderName = GetProviderName(providerId),
                        IsConnected = true,
                        UserToken = token
                    });
                }
                return true;
            }
            return false;
        }

        public async Task<bool> DisconnectProviderAsync(int userId, string providerId)
        {
            await Task.Delay(100);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                var account = user.ConnectedAccounts.FirstOrDefault(a => a.ProviderId == providerId);
                if (account != null)
                {
                    account.IsConnected = false;
                    account.UserToken = string.Empty;
                    return true;
                }
            }
            return false;
        }

        public async Task RecordSurveyCompletionAsync(int userId, ExternalSurvey survey)
        {
            await Task.Delay(100);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.CompletedSurveys.Add(new CompletedSurvey
                {
                    SurveyId = survey.Id,
                    ProviderId = survey.ProviderId,
                    Title = survey.Title,
                    Reward = survey.Reward,
                    CompletedDate = DateTime.Now
                });

                var account = user.ConnectedAccounts.FirstOrDefault(a => a.ProviderId == survey.ProviderId);
                if (account != null)
                {
                    account.EarningsFromProvider += survey.Reward;
                }

                user.TotalEarnings += survey.Reward;
            }
        }

        // ADD THESE MISSING METHODS:
        public async Task UpdateUserBalanceAsync(int userId, decimal amount)
        {
            await Task.Delay(50);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.TotalEarnings += amount;
            }
        }

        public async Task MarkSurveyCompletedAsync(int userId, int surveyId)
        {
            await Task.Delay(50);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                // Add the survey to completed surveys if not already there
                var existingSurvey = user.CompletedSurveys.FirstOrDefault(s => s.SurveyId == $"pollfish_{surveyId}");
                if (existingSurvey == null)
                {
                    user.CompletedSurveys.Add(new CompletedSurvey
                    {
                        SurveyId = $"pollfish_{surveyId}",
                        ProviderId = "pollfish",
                        Title = "Survey Completed",
                        Reward = 0, // Will be updated by webhook
                        CompletedDate = DateTime.Now,
                        Status = "Completed"
                    });
                }
            }
        }

        private string GetProviderName(string providerId) => providerId switch
        {
            "pollfish" => "Pollfish",
            "dynata" => "Dynata",
            "lucid" => "Lucid (Cint)",
            "surveymonkey" => "SurveyMonkey Audience",
            _ => providerId
        };

        public User? GetUser(int id) => _users.FirstOrDefault(u => u.Id == id);
    }
}
