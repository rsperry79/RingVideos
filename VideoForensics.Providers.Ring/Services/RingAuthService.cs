using Microsoft.Extensions.Logging;
using VideoForensics.Providers.Common.Contracts;

namespace VideoForensics.Providers.Ring.Services
{
    public class RingAuthService : IProviderAuthService
    {
        private readonly ILogger _logger;
        private Session? _session;
        private bool _isAuthenticated;

        public RingAuthService(ILogger logger)
        {
            _logger = logger;
            _isAuthenticated = false;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Authenticating with Ring API for user: {Username}", username);

                _session = new Session(username, password);
                var response = await _session.Authenticate();

                if (response?.Profile != null)
                {
                    _isAuthenticated = true;
                    var expiresAt = DateTime.UtcNow.AddHours(24);

                    return new AuthResult(
                        Success: true,
                        AuthToken: _session.OAuthToken?.AccessToken,
                        ExpiresAt: expiresAt
                    );
                }

                _isAuthenticated = false;
                return new AuthResult(
                    Success: false,
                    ErrorMessage: "Authentication failed - no profile returned"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error");
                _isAuthenticated = false;
                return new AuthResult(
                    Success: false,
                    ErrorMessage: $"Authentication failed: {ex.Message}"
                );
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_session == null || !_isAuthenticated)
                return false;

            try
            {
                await _session.EnsureSessionValid();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RefreshAuthAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing Ring API token");

                if (_session == null)
                    return false;

                await _session.RefreshSession();
                _isAuthenticated = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return false;
            }
        }

        public string GetAuthStatus()
        {
            if (!_isAuthenticated)
                return "Not authenticated";

            if (_session?.OAuthToken == null)
                return "No session";

            return "Authenticated";
        }
    }
}
