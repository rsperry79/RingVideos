using Microsoft.Extensions.Logging;
using VideoForensics.Providers.Common.Contracts;
using VideoForensics.Providers.Core;
using VideoForensics.Providers.Ring.Services;

namespace VideoForensics.Providers.Ring
{
    public class RingVideoProvider : BaseVideoProvider
    {
        public override string ProviderName => "Ring";
        public override IProviderAuthService AuthService { get; }
        public override IDeviceDiscoveryService DeviceService { get; }
        public override IMediaDownloadService DownloadService { get; }
        public override IEventAndConfigService EventService { get; }

        public RingVideoProvider(ILogger logger, Session session)
            : base(logger)
        {
            AuthService = new RingAuthService(logger);
            DeviceService = new RingDeviceDiscoveryService(logger, session);
            DownloadService = new RingMediaDownloadService(logger, session);
            EventService = new RingEventAndConfigService(logger, session);
        }
    }
}
