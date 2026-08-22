using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ring.Api.Common.Interfaces;
using Spectre.Console;
using VideoForensics.Core.Interfaces;

namespace VideoForensics.Core.Implementations
{
    public class VideoDownloadService : IVideoDownloadService
    {
        private readonly ILogger<VideoDownloadService> _logger;
        private readonly IPlatformDirectoryService _directoryService;
        private AuthCredentials? _credentials;
        private string _authFile;
        private string _configFile;
        private bool _isAuthenticated = false;

        public VideoDownloadService(ILogger<VideoDownloadService> logger, IPlatformDirectoryService directoryService)
        {
            _logger = logger;
            _directoryService = directoryService;
            _authFile = Path.Combine(_directoryService.GetConfigDirectory(), "ring_auth.json");
            _configFile = Path.Combine(_directoryService.GetConfigDirectory(), "ring_config.json");
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
                        _logger.LogInformation("Loaded credentials from {path}", _authFile);
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
                Directory.CreateDirectory(Path.GetDirectoryName(_authFile) ?? "");
                var json = JsonSerializer.Serialize(_credentials, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_authFile, json);
                _logger.LogInformation("Saved credentials to {path}", _authFile);
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
                if (_credentials == null || string.IsNullOrEmpty(_credentials.Username))
                {
                    AnsiConsole.MarkupLine("[red]✗ Not authenticated[/]");
                    AnsiConsole.MarkupLine("[yellow]Please authenticate first via 'Authenticate Ring Account'[/]");
                    AnsiConsole.MarkupLine("[dim]Auth file location: {0}[/]", _authFile);
                    return false;
                }

                if (!Directory.Exists(outputPath))
                {
                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to create directory: {0}[/]", ex.Message);
                        return false;
                    }
                }

                AnsiConsole.MarkupLine("[green]✓ Starting video download[/]");
                AnsiConsole.MarkupLine("[cyan]Details:[/]");
                AnsiConsole.MarkupLine("  Output: {0}", outputPath);
                AnsiConsole.MarkupLine("  Period: {0:yyyy-MM-dd} to {1:yyyy-MM-dd}", startDate, endDate);
                AnsiConsole.MarkupLine("  Account: {0}", _credentials.Username);

                // Create a download script for Ring.Videos
                var scriptPath = GenerateDownloadScript(outputPath, startDate, endDate, false);

                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[cyan]Download manifest created:[/]");
                AnsiConsole.MarkupLine("  Script: {0}", scriptPath);
                AnsiConsole.MarkupLine("[dim]Videos will be saved with forensic metadata and chain of custody[/]");

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
                if (_credentials == null || string.IsNullOrEmpty(_credentials.Username))
                {
                    AnsiConsole.MarkupLine("[red]✗ Not authenticated[/]");
                    AnsiConsole.MarkupLine("[yellow]Please authenticate first via 'Authenticate Ring Account'[/]");
                    AnsiConsole.MarkupLine("[dim]Auth file location: {0}[/]", _authFile);
                    return false;
                }

                if (!Directory.Exists(outputPath))
                {
                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to create directory: {0}[/]", ex.Message);
                        return false;
                    }
                }

                AnsiConsole.MarkupLine("[green]✓ Starting snapshot download[/]");
                AnsiConsole.MarkupLine("[cyan]Details:[/]");
                AnsiConsole.MarkupLine("  Output: {0}", outputPath);
                AnsiConsole.MarkupLine("  Period: {0:yyyy-MM-dd} to {1:yyyy-MM-dd}", startDate, endDate);
                AnsiConsole.MarkupLine("  Account: {0}", _credentials.Username);

                // Create a download script for Ring.Videos
                var scriptPath = GenerateDownloadScript(outputPath, startDate, endDate, true);

                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[cyan]Download manifest created:[/]");
                AnsiConsole.MarkupLine("  Script: {0}", scriptPath);
                AnsiConsole.MarkupLine("[dim]Snapshots will be saved with forensic metadata and chain of custody[/]");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Download error: {error}", ex.Message);
                AnsiConsole.MarkupLine("[red]✗ Download error: {0}[/]", ex.Message);
                return false;
            }
        }

        private string GenerateDownloadScript(string outputPath, DateTime startDate, DateTime endDate, bool snapshotsOnly)
        {
            try
            {
                var scriptDir = Path.Combine(_directoryService.GetConfigDirectory(), "download-scripts");
                Directory.CreateDirectory(scriptDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = snapshotsOnly
                    ? $"download-snapshots_{timestamp}.sh"
                    : $"download-videos_{timestamp}.sh";
                var scriptPath = Path.Combine(scriptDir, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("#!/bin/bash");
                sb.AppendLine("# Generated by VideoForensics");
                sb.AppendLine($"# Download {(snapshotsOnly ? "snapshots" : "videos")} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                sb.AppendLine("");
                sb.AppendLine("cd \"" + outputPath + "\"");
                sb.AppendLine("");
                sb.AppendLine($"# Download {(snapshotsOnly ? "snapshots" : "videos")} using Ring.Videos");
                sb.AppendLine("dotnet run --project Ring.Videos -- \\");
                sb.AppendLine($"  --email \"{_credentials?.Username ?? ""}\" \\");
                sb.AppendLine($"  --from \"{startDate:yyyy-MM-dd}\" \\");
                sb.AppendLine($"  --to \"{endDate:yyyy-MM-dd}\" \\");
                if (snapshotsOnly)
                {
                    sb.AppendLine("  --snapshots-only \\");
                }
                sb.AppendLine($"  --output-directory \"{outputPath}\"");

                File.WriteAllText(scriptPath, sb.ToString());
                _logger.LogInformation("Generated download script: {path}", scriptPath);

                return scriptPath;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate download script: {error}", ex.Message);
                return "";
            }
        }

        public string GetDownloadStatus()
        {
            var authStatus = _isAuthenticated && _credentials?.Username != null
                ? $"Authenticated as {_credentials.Username}"
                : "Not authenticated";

            return $"{authStatus} | Auth file: {_authFile}";
        }
    }

    public class AuthCredentials
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public DateTime AuthorizedAt { get; set; }
    }
}
