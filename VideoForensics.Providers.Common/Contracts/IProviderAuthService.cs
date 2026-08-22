namespace VideoForensics.Providers.Common.Contracts
{
    /// <summary>Platform-agnostic authentication interface for video providers</summary>
    public interface IProviderAuthService
    {
        /// <summary>Authenticates with the provider using credentials</summary>
        Task<AuthResult> AuthenticateAsync(string username, string password);

        /// <summary>Checks if currently authenticated</summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>Refreshes authentication if needed</summary>
        Task<bool> RefreshAuthAsync();

        /// <summary>Gets current authentication status</summary>
        string GetAuthStatus();
    }

    /// <summary>Result of authentication attempt</summary>
    public record AuthResult(
        bool Success,
        string? ErrorMessage = null,
        string? AuthToken = null,
        DateTime? ExpiresAt = null
    );
}
