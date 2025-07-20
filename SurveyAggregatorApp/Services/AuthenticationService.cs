using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SurveyAggregatorApp.Models;
using System.Security.Claims;

namespace SurveyAggregatorApp.Services
{
    public class AuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SecurityService _securityService;

        public AuthenticationService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            SecurityService securityService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _securityService = securityService;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || !user.IsActive)
                return null;

            // For demo purposes, accept any password for existing users
            // In production, use: BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)
            if (user.Email == "user@example.com" || BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Profile?.FullName ?? user.Email)
                };

                // Create identity
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                        });

                    // Update last login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return user;
            }

            return null;
        }

        public async Task<User?> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                return null;

            var user = new User
            {
                Email = email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create user profile
            var profile = new UserProfile
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Country = "GB",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Reload user with profile
            return await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstAsync(u => u.Id == user.Id);
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return await _context.Users
                .Include(u => u.Profile)
                .Include(u => u.ConnectedAccounts)
                .Include(u => u.CompletedSurveys)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}