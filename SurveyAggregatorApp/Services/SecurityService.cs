// Services/SecurityService.cs - Security and rate limiting
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace SurveyAggregatorApp.Services
{
    public class SecurityService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(IMemoryCache cache, ILogger<SecurityService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool IsRateLimited(string identifier, string action, int maxAttempts = 5, TimeSpan? timeWindow = null)
        {
            timeWindow ??= TimeSpan.FromMinutes(15);
            var key = $"ratelimit_{action}_{identifier}";

            if (_cache.TryGetValue(key, out int attempts))
            {
                if (attempts >= maxAttempts)
                {
                    _logger.LogWarning($"Rate limit exceeded for {identifier} on action {action}");
                    return true;
                }

                _cache.Set(key, attempts + 1, timeWindow.Value);
            }
            else
            {
                _cache.Set(key, 1, timeWindow.Value);
            }

            return false;
        }

        public void ResetRateLimit(string identifier, string action)
        {
            var key = $"ratelimit_{action}_{identifier}";
            _cache.Remove(key);
        }

        public string GenerateSecureToken(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")[..length];
        }

        public string HashSensitiveData(string data, string salt = "")
        {
            if (string.IsNullOrEmpty(salt))
            {
                salt = GenerateSecureToken(16);
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(data + salt);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool ValidateApiKey(string apiKey, string expectedHash)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(expectedHash))
                return false;

            // In production, use proper API key validation
            return BCrypt.Net.BCrypt.Verify(apiKey, expectedHash);
        }

        public async Task<bool> IsValidWebhookSignatureAsync(string payload, string signature, string secret)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToBase64String(computedHash);

                return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                return false;
            }
        }
    }

    // Middleware for request logging and security
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityService _securityService;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, SecurityService securityService, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _securityService = securityService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var path = context.Request.Path;

            // Log request
            _logger.LogInformation($"Request: {context.Request.Method} {path} from {ipAddress}");

            // Rate limiting for sensitive endpoints
            if (IsSensitiveEndpoint(path))
            {
                if (_securityService.IsRateLimited(ipAddress, "api_call", 100, TimeSpan.FromHours(1)))
                {
                    context.Response.StatusCode = 429; // Too Many Requests
                    await context.Response.WriteAsync("Rate limit exceeded");
                    return;
                }
            }

            // Security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            await _next(context);
        }

        private bool IsSensitiveEndpoint(string path)
        {
            var sensitiveEndpoints = new[] { "/api/", "/admin/", "/webhook/" };
            return sensitiveEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
        }
    }
}
