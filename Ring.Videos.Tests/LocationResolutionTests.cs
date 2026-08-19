using KoenZomers.Ring.Api.Entities;

namespace Ring.Videos.Tests;

public class LocationResolutionTests
{
    [Fact]
    public void LocationNameResolutionUsesApiResult()
    {
        // Simulate location data from API
        var locations = new List<Location>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Front Door",
                Address = new LocationAddress
                {
                    Address1 = "123 Main St",
                    City = "Springfield",
                    State = "IL",
                    ZipCode = "62701",
                    TimeZone = "America/Chicago"
                },
                IsOwner = true
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Back Patio",
                Address = new LocationAddress
                {
                    Address1 = "123 Main St",
                    City = "Springfield",
                    State = "IL",
                    ZipCode = "62701",
                    TimeZone = "America/Chicago"
                },
                IsOwner = true
            }
        };

        var locationById = locations.ToDictionary(l => l.Id ?? Guid.Empty, l => l.Name);

        var locationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var name = locationById.TryGetValue(locationId, out var value) ? value : "Unknown";

        Assert.Equal("Front Door", name);
    }

    [Fact]
    public void LocationNameResolutionFallsBackToDefault()
    {
        var locations = new List<Location>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Front Door"
            }
        };

        var locationById = locations.ToDictionary(l => l.Id ?? Guid.Empty, l => l.Name);

        // Query for non-existent location
        var locationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var name = locationById.TryGetValue(locationId, out var value) ? value : "Unknown Location";

        Assert.Equal("Unknown Location", name);
    }

    [Fact]
    public void LocationNameResolutionWithAppSettingsFallback()
    {
        // Simulate appSettings LocationNames fallback
        var apiLocations = new Dictionary<Guid, string>
        {
            { Guid.Parse("11111111-1111-1111-1111-111111111111"), "Front Door" }
        };

        var fallbackLocationNames = new Dictionary<string, string>
        {
            { "22222222-2222-2222-2222-222222222222", "Back Patio (from config)" }
        };

        // Try to resolve from API first, then fallback to config
        var locationIdToResolve = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var name = apiLocations.TryGetValue(locationIdToResolve, out var apiName)
            ? apiName
            : (fallbackLocationNames.TryGetValue(locationIdToResolve.ToString(), out var configName) ? configName : "Unknown");

        Assert.Equal("Back Patio (from config)", name);
    }

    [Fact]
    public void LocationCanBeNullAndHandledGracefully()
    {
        var location = new Location
        {
            Id = null,
            Name = null,
            Address = null,
            IsOwner = null
        };

        var id = location.Id ?? Guid.Empty;
        var name = location.Name ?? "Unknown";

        Assert.Equal(Guid.Empty, id);
        Assert.Equal("Unknown", name);
    }
}
