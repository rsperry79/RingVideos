using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VideoForensics.Core;
using VideoForensics.Core.Implementations;
using VideoForensics.Core.Interfaces;
using Xunit;

namespace VideoForensics.Core.Tests
{
    public class ForensicsFactoryTests : IDisposable
    {
        private readonly string _testConfigDir;

        public ForensicsFactoryTests()
        {
            _testConfigDir = Path.Combine(Path.GetTempPath(), $"videoforensics-factory-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testConfigDir);
        }

        [Fact]
        public void LoadConfiguration_WithNonexistentFile_ReturnsDefaultConfiguration()
        {
            // Arrange
            var configPath = Path.Combine(_testConfigDir, "nonexistent.json");

            // Act
            var config = ForensicsFactory.LoadConfiguration(configPath);

            // Assert
            Assert.NotNull(config);
            Assert.True(config.EnableForensicAnalysisReports);
            Assert.Equal("json", config.ReportOutputFormat);
            Assert.Equal(365, config.RetentionDaysDefault);
        }

        [Fact]
        public void LoadConfiguration_WithValidFile_ReturnsLoadedConfiguration()
        {
            // Arrange
            var configPath = Path.Combine(_testConfigDir, "config.json");
            var originalConfig = new ForensicsConfiguration
            {
                EnableForensicAnalysisReports = false,
                RedactionLevel = RedactionLevel.Heavy,
                DownloadLocation = "/tmp/downloads",
                RetentionDaysDefault = 90
            };

            var json = JsonSerializer.Serialize(originalConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            // Act
            var loadedConfig = ForensicsFactory.LoadConfiguration(configPath);

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.False(loadedConfig.EnableForensicAnalysisReports);
            Assert.Equal(RedactionLevel.Heavy, loadedConfig.RedactionLevel);
            Assert.Equal("/tmp/downloads", loadedConfig.DownloadLocation);
            Assert.Equal(90, loadedConfig.RetentionDaysDefault);
        }

        [Fact]
        public void LoadConfiguration_WithInvalidJson_ReturnsDefaultConfiguration()
        {
            // Arrange
            var configPath = Path.Combine(_testConfigDir, "invalid.json");
            File.WriteAllText(configPath, "{ invalid json }");

            // Act
            var config = ForensicsFactory.LoadConfiguration(configPath);

            // Assert
            Assert.NotNull(config);
            // Should return default config when deserialization fails
            Assert.Equal("json", config.ReportOutputFormat);
        }

        [Fact]
        public void CreateVideoDownloadService_ReturnsValidService()
        {
            // Act
            var service = ForensicsFactory.CreateVideoDownloadService();

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<IVideoDownloadService>(service);
        }

        [Fact]
        public void CreateVideoDownloadService_ReturnsDifferentInstances()
        {
            // Act
            var service1 = ForensicsFactory.CreateVideoDownloadService();
            var service2 = ForensicsFactory.CreateVideoDownloadService();

            // Assert
            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void CreateMenuManager_ReturnsValidManager()
        {
            // Arrange
            var config = new ForensicsConfiguration();
            var configPath = Path.Combine(_testConfigDir, "config.json");
            var downloadService = ForensicsFactory.CreateVideoDownloadService();

            // Act
            var manager = ForensicsFactory.CreateMenuManager(config, configPath, downloadService);

            // Assert
            Assert.NotNull(manager);
            Assert.IsAssignableFrom<IMenuManager>(manager);
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
