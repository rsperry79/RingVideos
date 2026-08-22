using Microsoft.Extensions.Logging;
using VideoForensics.Providers.Common.Contracts;

namespace VideoForensics.Providers.Ring.Services
{
    public class RingDeviceDiscoveryService : IDeviceDiscoveryService
    {
        private readonly ILogger _logger;
        private readonly Session _session;

        public RingDeviceDiscoveryService(ILogger logger, Session session)
        {
            _logger = logger;
            _session = session;
        }

        public async Task<IReadOnlyList<Location>> GetLocationsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Ring locations");

                var locations = await _session.GetLocations();

                var result = locations
                    .Where(l => l.Id.HasValue)
                    .Select(l => new Location(
                        Id: l.Id!.Value.ToString(),
                        Name: l.Name ?? "Unknown Location",
                        Address: l.Address?.Address1
                    ))
                    .ToList();

                _logger.LogInformation("Found {LocationCount} locations", result.Count);
                return result.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching locations");
                return new List<Location>().AsReadOnly();
            }
        }

        public async Task<IReadOnlyList<Device>> GetDevicesAsync(string locationId)
        {
            try
            {
                _logger.LogInformation("Fetching devices for location: {LocationId}", locationId);

                if (!Guid.TryParse(locationId, out var locId))
                {
                    return new List<Device>().AsReadOnly();
                }

                var devices = await _session.GetRingDevices(locId);

                var locationDevices = devices?.Doorbots?
                    .Select(d => new Device(
                        Id: d.DeviceId,
                        Name: d.Description ?? "Unknown Device",
                        Type: "doorbot",
                        LocationId: d.LocationId?.ToString() ?? locationId,
                        IsOnline: d.Subscribed ?? false
                    ))
                    .ToList() ?? new List<Device>();

                _logger.LogInformation("Found {DeviceCount} devices in location {LocationId}", locationDevices.Count, locationId);
                return locationDevices.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices for location {LocationId}", locationId);
                return new List<Device>().AsReadOnly();
            }
        }

        public async Task<Device?> GetDeviceAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Fetching device: {DeviceId}", deviceId);

                var locations = await GetLocationsAsync();

                foreach (var location in locations)
                {
                    var devices = await GetDevicesAsync(location.Id);
                    var device = devices.FirstOrDefault(d => d.Id == deviceId);
                    if (device != null)
                        return device;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device {DeviceId}", deviceId);
                return null;
            }
        }
    }
}
