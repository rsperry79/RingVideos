using System.Text.Json;
using VideoForensics.Core.Implementations;
using VideoForensics.Core.Interfaces;
using Xunit;

namespace VideoForensics.Core.Tests
{
    public class ForensicsConfigurationTests
    {
        [Fact]
        public void NewConfiguration_HasDefaultValues()
        {
            // Arrange & Act
            IForensicsConfiguration config = new ForensicsConfiguration();

            // Assert
            Assert.True(config.EnableForensicAnalysisReports);
            Assert.True(config.EnableSignalAnomalyReports);
            Assert.True(config.EnableChainOfCustodyReports);
            Assert.True(config.EnableEvidenceValidationReports);
            Assert.True(config.EnableAccessControlMonitoring);
            Assert.True(config.EnableMultiDeviceAnalysis);
            Assert.True(config.EnablePiiRedaction);
            Assert.Equal(RedactionLevel.Medium, config.RedactionLevel);
            Assert.Equal(KeyStorageProvider.Auto, config.KeyStorageProvider);
            Assert.Equal(365, config.RetentionDaysDefault);
            Assert.Equal("json", config.ReportOutputFormat);
            Assert.Equal("Information", config.LogLevel);
            Assert.Empty(config.ReportsDirectory);
            Assert.Empty(config.DownloadLocation);
        }

        [Fact]
        public void Configuration_CanBeModified()
        {
            // Arrange
            IForensicsConfiguration config = new ForensicsConfiguration();

            // Act
            config.EnableForensicAnalysisReports = false;
            config.RedactionLevel = RedactionLevel.Heavy;
            config.RetentionDaysDefault = 180;
            config.DownloadLocation = "/tmp/downloads";

            // Assert
            Assert.False(config.EnableForensicAnalysisReports);
            Assert.Equal(RedactionLevel.Heavy, config.RedactionLevel);
            Assert.Equal(180, config.RetentionDaysDefault);
            Assert.Equal("/tmp/downloads", config.DownloadLocation);
        }

        [Fact]
        public void Configuration_CanBeSerialized()
        {
            // Arrange
            var config = new ForensicsConfiguration
            {
                EnableForensicAnalysisReports = false,
                DownloadLocation = "/home/user/downloads"
            };

            // Act
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert - verify JSON contains key properties
            Assert.NotEmpty(json);
            Assert.Contains("enableForensicAnalysisReports", json);
            Assert.Contains("false", json);
            Assert.Contains("downloadLocation", json);
            Assert.Contains("/home/user/downloads", json);
        }

        [Fact]
        public void Configuration_CanBeDeserialized()
        {
            // Arrange
            var json = @"{
  ""enableForensicAnalysisReports"": false,
  ""enableSignalAnomalyReports"": true,
  ""enableChainOfCustodyReports"": true,
  ""enableEvidenceValidationReports"": true,
  ""enableAccessControlMonitoring"": true,
  ""enableMultiDeviceAnalysis"": true,
  ""enablePiiRedaction"": true,
  ""redactionLevel"": 2,
  ""keyStorageProvider"": 1,
  ""retentionDaysDefault"": 180,
  ""reportOutputFormat"": ""xml"",
  ""reportsDirectory"": ""/var/reports"",
  ""logLevel"": ""Debug"",
  ""downloadLocation"": ""/tmp/downloads""
}";

            // Act
            var config = JsonSerializer.Deserialize<ForensicsConfiguration>(json);

            // Assert
            Assert.NotNull(config);
            Assert.False(config.EnableForensicAnalysisReports);
            Assert.True(config.EnableSignalAnomalyReports);
            Assert.Equal(RedactionLevel.Medium, config.RedactionLevel);
            Assert.Equal(KeyStorageProvider.Tpm, config.KeyStorageProvider);
            Assert.Equal(180, config.RetentionDaysDefault);
            Assert.Equal("xml", config.ReportOutputFormat);
            Assert.Equal("Debug", config.LogLevel);
            Assert.Equal("/tmp/downloads", config.DownloadLocation);
        }

        [Theory]
        [InlineData(RedactionLevel.None)]
        [InlineData(RedactionLevel.Light)]
        [InlineData(RedactionLevel.Medium)]
        [InlineData(RedactionLevel.Heavy)]
        public void Configuration_SupportsAllRedactionLevels(RedactionLevel level)
        {
            // Arrange
            IForensicsConfiguration config = new ForensicsConfiguration();

            // Act
            config.RedactionLevel = level;

            // Assert
            Assert.Equal(level, config.RedactionLevel);
        }

        [Theory]
        [InlineData(KeyStorageProvider.Auto)]
        [InlineData(KeyStorageProvider.Tpm)]
        [InlineData(KeyStorageProvider.Dpapi)]
        [InlineData(KeyStorageProvider.FileBased)]
        public void Configuration_SupportsAllKeyStorageProviders(KeyStorageProvider provider)
        {
            // Arrange
            IForensicsConfiguration config = new ForensicsConfiguration();

            // Act
            config.KeyStorageProvider = provider;

            // Assert
            Assert.Equal(provider, config.KeyStorageProvider);
        }
    }
}
