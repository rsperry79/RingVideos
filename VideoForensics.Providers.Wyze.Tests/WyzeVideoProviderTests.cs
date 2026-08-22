using Microsoft.Extensions.Logging;
using Moq;
using VideoForensics.Providers.Common.Contracts;
using Xunit;

namespace VideoForensics.Providers.Wyze.Tests
{
    /// <summary>
    /// Tests for WyzeVideoProvider implementation.
    /// Verifies provider initialization and interface implementation.
    /// </summary>
    public class WyzeVideoProviderTests
    {
        [Fact]
        public void WyzeVideoProvider_HasCorrectProviderName()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();

            // Act
            var provider = new WyzeVideoProvider(mockLogger.Object);

            // Assert
            Assert.Equal("Wyze", provider.ProviderName);
        }

        [Fact]
        public void WyzeVideoProvider_ImplementsIVideoProvider()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();

            // Act
            var provider = new WyzeVideoProvider(mockLogger.Object);

            // Assert
            Assert.IsAssignableFrom<IVideoProvider>(provider);
        }

        [Fact]
        public void WyzeVideoProvider_HasAllRequiredServices()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();

            // Act
            var provider = new WyzeVideoProvider(mockLogger.Object);

            // Assert
            Assert.NotNull(provider.AuthService);
            Assert.NotNull(provider.DeviceService);
            Assert.NotNull(provider.DownloadService);
            Assert.NotNull(provider.EventService);

            Assert.IsAssignableFrom<IProviderAuthService>(provider.AuthService);
            Assert.IsAssignableFrom<IDeviceDiscoveryService>(provider.DeviceService);
            Assert.IsAssignableFrom<IMediaDownloadService>(provider.DownloadService);
            Assert.IsAssignableFrom<IEventAndConfigService>(provider.EventService);
        }

        [Fact]
        public void WyzeVideoProvider_ServiceImplementationsAreNotNull()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();

            // Act
            var provider = new WyzeVideoProvider(mockLogger.Object);

            // Assert
            // Verify each service is correctly wired
            var authService = provider.AuthService as Services.WyzeAuthService;
            Assert.NotNull(authService);

            var deviceService = provider.DeviceService as Services.WyzeDeviceDiscoveryService;
            Assert.NotNull(deviceService);

            var downloadService = provider.DownloadService as Services.WyzeMediaDownloadService;
            Assert.NotNull(downloadService);

            var eventService = provider.EventService as Services.WyzeEventAndConfigService;
            Assert.NotNull(eventService);
        }
    }
}
