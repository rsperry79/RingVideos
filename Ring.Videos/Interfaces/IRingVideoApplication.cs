using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using KoenZomers.Ring.Api.Entities;

using Ring.Videos.Models;

namespace Ring.Videos.Interfaces;

/// <summary>
/// Main application interface for Ring video operations.
/// </summary>
public interface IRingVideoApplication
{
    /// <summary>
    /// Authenticates with the Ring API.
    /// </summary>
    Task<bool> AuthenticateAsync();

    /// <summary>
    /// Downloads videos matching the specified filter.
    /// </summary>
    Task<int> DownloadVideosAsync(Filter filter);

    /// <summary>
    /// Gets a formatted message describing the applied filter.
    /// </summary>
    string GetFilterMessage();

    /// <summary>
    /// Gets all available devices for the user.
    /// </summary>
    Task<List<DeviceInfo>> GetDevicesAsync();

    /// <summary>
    /// Gets detailed information about a specific device.
    /// </summary>
    Task<DeviceInfo> GetDeviceInfoAsync(string deviceId);

    /// <summary>
    /// Gets all locations accessible by the user.
    /// </summary>
    Task<List<Location>> GetLocationsAsync();

    /// <summary>
    /// Applies filters and gets matching events.
    /// </summary>
    Task<List<DoorbotHistoryEvent>> GetFilteredEventsAsync(Filter filter);
}
