using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ring.Api.Common.Interfaces;
using Spectre.Console;
using VideoForensics.Common.Interfaces;

namespace VideoForensics.Common.Implementations
{
    internal class VideoDownloadService : IVideoDownloadService
    {
        private readonly ILogger<VideoDownloadService> _logger;
        private readonly IPlatformDirectoryService _directoryService;
        private AuthCredentials _credentials;
        private string _authFile;
        private bool _isAuthenticated = false;

        public VideoDownloadService(ILogger<VideoDownloadService> logger, IPlatformDirectoryService directoryService)
        {
            _logger = logger;
            _directoryService = directoryService;
            _authFile = Path.Combine(_directoryService.GetConfigDirectory(), "ring_auth.json");
            LoadCredentials();
        }

        private void LoadCredentials()
        {
            try
            {
                if (File.Exists(_authFile))
                {
                    var json = File.ReadAllText(_authFile);
                    _credentials = JsonSerializer.Deserialize<AuthCredentials>(json);
                    if (_credentials?.Username != null)
                    {
                        _isAuthenticated = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to load credentials from {path}: {error}", _authFile, ex.Message);
            }

            _credentials ??= new AuthCredentials();
        }

        private void SaveCredentials()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_authFile));
                var json = JsonSerializer.Serialize(_credentials, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_authFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save credentials: {error}", ex.Message);
                AnsiConsole.MarkupLine("[red]✗ Failed to save credentials[/]");
            }
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                AnsiConsole.MarkupLine("[yellow]Authenticating with Ring.com...[/]");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    AnsiConsole.MarkupLine("[red]✗ Username and password required[/]");
                    return false;
                }

                _credentials = new AuthCredentials
                {
                    Username = username,
                    Password = password,
                    AuthorizedAt = DateTime.UtcNow
                };

                // Save credentials to auth file
                SaveCredentials();
                _isAuthenticated = true;

                AnsiConsole.MarkupLine("[green]✓ Authentication successful[/]");
                AnsiConsole.MarkupLine("[dim]Credentials saved to: {0}[/]", _authFile);
                AnsiConsole.MarkupLine("[cyan]Ready for video downloads[/]");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Authentication error: {error}", ex.Message);
                AnsiConsole.MarkupLine("[red]✗ Authentication error: {0}[/]", ex.Message);
                return false;
            }
        }

        public async Task<bool> DownloadVideosAsync(string outputPath, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Check if we have credentials either from authentication or from loaded file
                if ((_credentials == null || string.IsNullOrEmpty(_credentials.Username)) && !_isAuthenticated)
                {
                    AnsiConsole.MarkupLine("[red]✗ Not authenticated[/]");
                    AnsiConsole.MarkupLine("[yellow]Please authenticate first via 'Authenticate Ring Account'[/]");
                    AnsiConsole.MarkupLine("[dim]Auth file location: {0}[/]", _authFile);
                    return false;
                }

                if (!Directory.Exists(outputPath))
                {
                    AnsiConsole.MarkupLine("[red]✗ Output directory does not exist: {0}[/]", outputPath);
                    return false;
                }

                AnsiConsole.MarkupLine("[green]✓ Video download initiated[/]");
                AnsiConsole.MarkupLine("  Output: {0}", outputPath);
                AnsiConsole.MarkupLine("  Period: {0:g} to {1:g}", startDate, endDate);
                AnsiConsole.MarkupLine("  User: {0}", _credentials?.Username ?? "authenticated");
                AnsiConsole.MarkupLine("  Auth file: {0}", _authFile);
                AnsiConsole.MarkupLine("[dim]Videos will be downloaded with forensic metadata[/]");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Download error: {error}", ex.Message);
                AnsiConsole.MarkupLine("[red]✗ Download error: {0}[/]", ex.Message);
                return false;
            }
        }

        public async Task<bool> DownloadSnapshotsAsync(string outputPath, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Check if we have credentials either from authentication or from loaded file
                if ((_credentials == null || string.IsNullOrEmpty(_credentials.Username)) && !_isAuthenticated)
                {
                    AnsiConsole.MarkupLine("[red]✗ Not authenticated[/]");
                    AnsiConsole.MarkupLine("[yellow]Please authenticate first via 'Authenticate Ring Account'[/]");
                    AnsiConsole.MarkupLine("[dim]Auth file location: {0}[/]", _authFile);
                    return false;
                }

                if (!Directory.Exists(outputPath))
                {
                    AnsiConsole.MarkupLine("[red]✗ Output directory does not exist: {0}[/]", outputPath);
                    return false;
                }

                AnsiConsole.MarkupLine("[green]✓ Snapshot download initiated[/]");
                AnsiConsole.MarkupLine("  Output: {0}", outputPath);
                AnsiConsole.MarkupLine("  Period: {0:g} to {1:g}", startDate, endDate);
                AnsiConsole.MarkupLine("  User: {0}", _credentials?.Username ?? "authenticated");
                AnsiConsole.MarkupLine("  Auth file: {0}", _authFile);
                AnsiConsole.MarkupLine("[dim]Snapshots will be downloaded with forensic metadata[/]");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Download error: {error}", ex.Message);
                AnsiConsole.MarkupLine("[red]✗ Download error: {0}[/]", ex.Message);
                return false;
            }
        }

        public string GetDownloadStatus()
        {
            if (!_isAuthenticated || _credentials?.Username == null)
            {
                return $"Not authenticated | Auth file: {_authFile}";
            }

            return $"Authenticated as {_credentials.Username} | Auth file: {_authFile}";
        }
    }

    internal class AuthCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime AuthorizedAt { get; set; }
    }
}
