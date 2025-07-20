using Microsoft.EntityFrameworkCore;
using SurveyAggregatorApp.Models;

namespace SurveyAggregatorApp.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly SecurityService _securityService;

        public UserService(ApplicationDbContext context, SecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user == null) return null;

            // For demo purposes, accept any password for the demo user
            if (email == "user@example.com" || BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return user;
            }

            return null;
        }

        public async Task<User> RegisterAsync(string email, string name)
        {
            var nameParts = name.Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var user = new User
            {
                Email = email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("defaultpassword"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var profile = new UserProfile
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Country = "GB"
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return await GetUserWithDetailsAsync(user.Id);
        }

        public async Task<bool> ConnectProviderAsync(int userId, string providerId, string token, string externalUserId = "")
        {
            var existingAccount = await _context.ProviderAccounts
                .FirstOrDefaultAsync(pa => pa.UserId == userId && pa.ProviderId == providerId);

            if (existingAccount != null)
            {
                existingAccount.IsConnected = true;
                existingAccount.UserToken = token;
                existingAccount.ExternalUserId = externalUserId;
                existingAccount.ConnectedAt = DateTime.UtcNow;
            }
            else
            {
                var newAccount = new ProviderAccount
                {
                    UserId = userId,
                    ProviderId = providerId,
                    ProviderName = GetProviderName(providerId),
                    IsConnected = true,
                    UserToken = token,
                    ExternalUserId = externalUserId,
                    ConnectedAt = DateTime.UtcNow
                };

                _context.ProviderAccounts.Add(newAccount);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DisconnectProviderAsync(int userId, string providerId)
        {
            var account = await _context.ProviderAccounts
                .FirstOrDefaultAsync(pa => pa.UserId == userId && pa.ProviderId == providerId);

            if (account != null)
            {
                account.IsConnected = false;
                account.UserToken = string.Empty;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> RecordSurveyCompletionAsync(int userId, ExternalSurvey survey)
        {
            // Check if already completed
            var existingSurvey = await _context.CompletedSurveys
                .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.SurveyId == survey.Id);

            if (existingSurvey != null)
                return false; // Already completed

            var completedSurvey = new CompletedSurvey
            {
                UserId = userId,
                SurveyId = survey.Id,
                ProviderId = survey.ProviderId,
                Title = survey.Title,
                Description = survey.Description,
                Reward = survey.Reward,
                CompletedAt = DateTime.UtcNow,
                Status = "Completed"
            };

            _context.CompletedSurveys.Add(completedSurvey);

            // Update user's total earnings
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalEarnings += survey.Reward;
            }

            // Update provider account earnings
            var providerAccount = await _context.ProviderAccounts
                .FirstOrDefaultAsync(pa => pa.UserId == userId && pa.ProviderId == survey.ProviderId);

            if (providerAccount != null)
            {
                providerAccount.EarningsFromProvider += survey.Reward;
                providerAccount.SurveysCompleted++;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetMonthlyEarningsAsync(int userId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await _context.CompletedSurveys
                .Where(cs => cs.UserId == userId &&
                           cs.CompletedAt >= startOfMonth &&
                           cs.CompletedAt < endOfMonth &&
                           cs.Status == "Completed")
                .SumAsync(cs => cs.Reward);
        }

        public async Task<Dictionary<string, decimal>> GetEarningsByProviderAsync(int userId)
        {
            return await _context.ProviderAccounts
                .Where(pa => pa.UserId == userId && pa.IsConnected)
                .ToDictionaryAsync(pa => pa.ProviderName, pa => pa.EarningsFromProvider);
        }

        public async Task<List<UserTransaction>> GetUserTransactionsAsync(int userId, int limit = 50)
        {
            return await _context.UserTransactions
                .Where(ut => ut.UserId == userId)
                .OrderByDescending(ut => ut.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task UpdateUserBalanceAsync(int userId, decimal amount)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalEarnings += amount;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkSurveyCompletedAsync(int userId, int surveyId)
        {
            var existingSurvey = await _context.CompletedSurveys
                .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.SurveyId == $"pollfish_{surveyId}");

            if (existingSurvey == null)
            {
                var completedSurvey = new CompletedSurvey
                {
                    UserId = userId,
                    SurveyId = $"pollfish_{surveyId}",
                    ProviderId = "pollfish",
                    Title = "Survey Completed",
                    Reward = 0,
                    CompletedAt = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.CompletedSurveys.Add(completedSurvey);
                await _context.SaveChangesAsync();
            }
        }

        public User? GetUser(int id)
        {
            return _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstOrDefault(u => u.Id == id);
        }

        private async Task<User> GetUserWithDetailsAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstAsync(u => u.Id == userId);
        }

        private string GetProviderName(string providerId) => providerId switch
        {
            "pollfish" => "Pollfish",
            "dynata" => "Dynata",
            "lucid" => "Lucid (Cint)",
            "surveymonkey" => "SurveyMonkey Audience",
            _ => providerId
        };

        // Seed demo data if no users exist
        public async Task EnsureDemoDataAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                var demoUser = new User
                {
                    Email = "user@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    TotalEarnings = 15.75m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(demoUser);
                await _context.SaveChangesAsync();

                var profile = new UserProfile
                {
                    UserId = demoUser.Id,
                    FirstName = "John",
                    LastName = "Doe",
                    Country = "GB"
                };

                _context.UserProfiles.Add(profile);

                var pollfishAccount = new ProviderAccount
                {
                    UserId = demoUser.Id,
                    ProviderId = "pollfish",
                    ProviderName = "Pollfish",
                    IsConnected = true,
                    EarningsFromProvider = 8.50m,
                    UserToken = "pollfish_demo_token_123",
                    ConnectedAt = DateTime.UtcNow.AddDays(-7)
                };

                _context.ProviderAccounts.Add(pollfishAccount);

                var completedSurveys = new[]
                {
                    new CompletedSurvey
                    {
                        UserId = demoUser.Id,
                        SurveyId = "pollfish_001",
                        ProviderId = "pollfish",
                        Title = "Consumer Preferences Study",
                        Reward = 1.50m,
                        CompletedAt = DateTime.UtcNow.AddDays(-1),
                        Status = "Completed"
                    },
                    new CompletedSurvey
                    {
                        UserId = demoUser.Id,
                        SurveyId = "pollfish_002",
                        ProviderId = "pollfish",
                        Title = "Brand Awareness Survey",
                        Reward = 2.25m,
                        CompletedAt = DateTime.UtcNow.AddDays(-3),
                        Status = "Completed"
                    }
                };

                _context.CompletedSurveys.AddRange(completedSurveys);
                await _context.SaveChangesAsync();
            }
        }
    }
}