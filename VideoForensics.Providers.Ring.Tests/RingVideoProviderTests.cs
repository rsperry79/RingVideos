using Microsoft.Extensions.Logging;
using Moq;
using VideoForensics.Providers.Common.Contracts;
using Xunit;

namespace VideoForensics.Providers.Ring.Tests
{
    public class RingVideoProviderTests
    {
        [Fact]
        public void RingVideoProvider_HasCorrectProviderName()
        {
            var mockLogger = new Mock<ILogger>();
            var session = new Session("testuser", "password");

            var provider = new RingVideoProvider(mockLogger.Object, session);

            Assert.Equal("Ring", provider.ProviderName);
        }

        [Fact]
        public void RingVideoProvider_ImplementsIVideoProvider()
        {
            var mockLogger = new Mock<ILogger>();
            var session = new Session("testuser", "password");

            var provider = new RingVideoProvider(mockLogger.Object, session);

            Assert.IsAssignableFrom<IVideoProvider>(provider);
        }

        [Fact]
        public void RingVideoProvider_HasAllRequiredServices()
        {
            var mockLogger = new Mock<ILogger>();
            var session = new Session("testuser", "password");

            var provider = new RingVideoProvider(mockLogger.Object, session);

            Assert.NotNull(provider.AuthService);
            Assert.NotNull(provider.DeviceService);
            Assert.NotNull(provider.DownloadService);
            Assert.NotNull(provider.EventService);

            Assert.IsAssignableFrom<IProviderAuthService>(provider.AuthService);
            Assert.IsAssignableFrom<IDeviceDiscoveryService>(provider.DeviceService);
            Assert.IsAssignableFrom<IMediaDownloadService>(provider.DownloadService);
            Assert.IsAssignableFrom<IEventAndConfigService>(provider.EventService);
        }
    }
}
