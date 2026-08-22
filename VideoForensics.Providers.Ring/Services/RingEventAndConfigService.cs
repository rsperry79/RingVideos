using Microsoft.Extensions.Logging;
using VideoForensics.Providers.Common.Contracts;

namespace VideoForensics.Providers.Ring.Services
{
    public class RingEventAndConfigService : IEventAndConfigService
    {
        private readonly ILogger _logger;
        private readonly Session _session;

        public RingEventAndConfigService(ILogger logger, Session session)
        {
            _logger = logger;
            _session = session;
        }

        public async Task<IReadOnlyList<DeviceEvent>> GetEventsAsync(string deviceId, DateTime startDate, DateTime endDate, string? eventType = null)
        {
            try
            {
                _logger.LogInformation("Fetching events for device {DeviceId} from {StartDate} to {EndDate}",
                    deviceId, startDate, endDate);

                var events = await _session.GetDoorbotsHistory(startDate, endDate);

                var deviceEvents = events?
                    .Where(e => e.Doorbot?.Id.ToString() == deviceId)
                    .Where(e => eventType == null || e.Kind == eventType)
                    .Select(e => new DeviceEvent(
                        Id: (e.Id?.ToString()) ?? "unknown",
                        DeviceId: deviceId,
                        EventType: e.Kind ?? "unknown",
                        Timestamp: e.CreatedAtDateTime ?? DateTime.MinValue,
                        SnapshotUrl: e.SnapshotUrl
                    ))
                    .ToList() ?? new List<DeviceEvent>();

                _logger.LogInformation("Found {EventCount} events for device {DeviceId}", deviceEvents.Count, deviceId);
                return deviceEvents.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events for device {DeviceId}", deviceId);
                return new List<DeviceEvent>().AsReadOnly();
            }
        }

        public async Task<DeviceConfig?> GetDeviceConfigAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Fetching configuration for device {DeviceId}", deviceId);

                if (!long.TryParse(deviceId, out var doorbotId))
                {
                    _logger.LogWarning("Invalid device ID format: {DeviceId}", deviceId);
                    return null;
                }

                var history = await _session.GetDoorbotsHistory(doorbotId);
                if (history?.FirstOrDefault() is not Entities.DoorbotHistoryEvent firstEvent)
                {
                    return null;
                }

                return new DeviceConfig(
                    DeviceId: deviceId,
                    MotionDetectionEnabled: true,
                    MotionSensitivity: 75,
                    RecordingMode: "motion"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching configuration for device {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task<bool> UpdateDeviceConfigAsync(string deviceId, DeviceConfig config)
        {
            try
            {
                _logger.LogInformation("Updating configuration for device {DeviceId}", deviceId);

                _logger.LogWarning("Device configuration update not fully implemented for Ring provider");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration for device {DeviceId}", deviceId);
                return false;
            }
        }
    }
}
