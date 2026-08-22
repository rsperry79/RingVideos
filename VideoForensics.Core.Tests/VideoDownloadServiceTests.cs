using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Ring.Api.Common.Interfaces;
using VideoForensics.Core;
using VideoForensics.Core.Implementations;
using VideoForensics.Core.Interfaces;
using Xunit;

namespace VideoForensics.Core.Tests
{
    public class VideoDownloadServiceTests : IDisposable
    {
        private readonly Mock<ILogger<VideoDownloadService>> _mockLogger;
        private readonly Mock<IPlatformDirectoryService> _mockDirectoryService;
        private readonly string _testConfigDir;
        private readonly VideoDownloadService _service;

        public VideoDownloadServiceTests()
        {
            _testConfigDir = Path.Combine(Path.GetTempPath(), $"videoforensics-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testConfigDir);

            _mockLogger = new Mock<ILogger<VideoDownloadService>>();
            _mockDirectoryService = new Mock<IPlatformDirectoryService>();
            _mockDirectoryService
                .Setup(x => x.GetConfigDirectory())
                .Returns(_testConfigDir);

            _service = new VideoDownloadService(_mockLogger.Object, _mockDirectoryService.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ShouldSucceed()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.True(result);
            var authFile = Path.Combine(_testConfigDir, "ring_auth.json");
            Assert.True(File.Exists(authFile));

            var json = File.ReadAllText(authFile);
            var credentials = JsonSerializer.Deserialize<dynamic>(json);
            Assert.NotNull(credentials);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyUsername_ShouldFail()
        {
            // Arrange
            var username = "";
            var password = "password123";

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyPassword_ShouldFail()
        {
            // Arrange
            var username = "test@example.com";
            var password = "";

            // Act
            var result = await _service.AuthenticateAsync(username, password);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DownloadVideosAsync_WithoutAuthentication_ShouldFail()
        {
            // Arrange
            var outputPath = Path.Combine(_testConfigDir, "videos");

            // Act
            var result = await _service.DownloadVideosAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DownloadVideosAsync_WithAuthentication_ShouldSucceed()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";
            var outputPath = Path.Combine(_testConfigDir, "videos");

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var result = await _service.DownloadVideosAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(outputPath));
        }

        [Fact]
        public async Task DownloadVideosAsync_CreatesOutputDirectory_IfNotExists()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";
            var outputPath = Path.Combine(_testConfigDir, "new-videos-dir");
            Assert.False(Directory.Exists(outputPath));

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var result = await _service.DownloadVideosAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(outputPath));
        }

        [Fact]
        public async Task DownloadSnapshotsAsync_WithoutAuthentication_ShouldFail()
        {
            // Arrange
            var outputPath = Path.Combine(_testConfigDir, "snapshots");

            // Act
            var result = await _service.DownloadSnapshotsAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DownloadSnapshotsAsync_WithAuthentication_ShouldSucceed()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";
            var outputPath = Path.Combine(_testConfigDir, "snapshots");

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var result = await _service.DownloadSnapshotsAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(outputPath));
        }

        [Fact]
        public async Task DownloadVideosAsync_GeneratesDownloadScript()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";
            var outputPath = Path.Combine(_testConfigDir, "videos");

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var result = await _service.DownloadVideosAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.True(result);
            var scriptDir = Path.Combine(_testConfigDir, "download-scripts");
            Assert.True(Directory.Exists(scriptDir));
            var scripts = Directory.GetFiles(scriptDir, "download-videos_*.sh");
            Assert.Single(scripts);

            var scriptContent = File.ReadAllText(scripts[0]);
            Assert.Contains("Ring.Videos", scriptContent);
            Assert.Contains(username, scriptContent);
        }

        [Fact]
        public async Task DownloadSnapshotsAsync_GeneratesCorrectScript()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";
            var outputPath = Path.Combine(_testConfigDir, "snapshots");

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var result = await _service.DownloadSnapshotsAsync(outputPath, DateTime.Now.AddDays(-7), DateTime.Now);

            // Assert
            Assert.True(result);
            var scriptDir = Path.Combine(_testConfigDir, "download-scripts");
            var scripts = Directory.GetFiles(scriptDir, "download-snapshots_*.sh");
            Assert.Single(scripts);

            var scriptContent = File.ReadAllText(scripts[0]);
            Assert.Contains("--snapshots-only", scriptContent);
        }

        [Fact]
        public void GetDownloadStatus_WithoutAuthentication_ReturnsNotAuthenticated()
        {
            // Act
            var status = _service.GetDownloadStatus();

            // Assert
            Assert.Contains("Not authenticated", status);
        }

        [Fact]
        public async Task GetDownloadStatus_WithAuthentication_ReturnsUsername()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";

            // Authenticate first
            await _service.AuthenticateAsync(username, password);

            // Act
            var status = _service.GetDownloadStatus();

            // Assert
            Assert.Contains(username, status);
        }

        [Fact]
        public async Task CredentialsArePersistedAndLoaded()
        {
            // Arrange
            var username = "test@example.com";
            var password = "password123";

            // Authenticate with first service instance
            await _service.AuthenticateAsync(username, password);

            // Create new service instance with same config directory
            var newService = new VideoDownloadService(_mockLogger.Object, _mockDirectoryService.Object);

            // Act
            var status = newService.GetDownloadStatus();

            // Assert
            Assert.Contains(username, status);
        }

        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testConfigDir))
            {
                Directory.Delete(_testConfigDir, recursive: true);
            }
        }
    }
}
