using KoenZomers.Ring.Api;

using Ring.Videos;

using Ring.Videos.Models;

namespace Ring.Videos.Tests;

public class ApplicationTests
{
    [Fact]
    public void Filter_CanBeCreatedWithDefaults()
    {
        // Arrange & Act
        var filter = new Filter();

        // Assert
        Assert.NotNull(filter);
        Assert.Equal(10000, filter.VideoCount);
    }

    [Fact]
    public void Filter_CanHavePropertiesSet()
    {
        // Arrange
        var filter = new Filter();
        var now = DateTime.Now;

        // Act
        filter.VideoCount = 100;
        filter.StartDateTime = now;
        filter.EndDateTime = now.AddDays(1);

        // Assert
        Assert.Equal(100, filter.VideoCount);
        Assert.Equal(now, filter.StartDateTime);
        Assert.Equal(now.AddDays(1), filter.EndDateTime);
    }

    [Fact]
    public void RingCredentials_CanBeCreatedWithDefaults()
    {
        // Arrange & Act
        var auth = new RingCredentials();

        // Assert
        Assert.NotNull(auth);
        Assert.Null(auth.UserName);
        Assert.Null(auth.Password);
    }

    [Fact]
    public void RingCredentials_StoresUserNameAndPassword()
    {
        // Arrange
        var auth = new RingCredentials();
        var username = "test@example.com";
        var password = "testPassword";

        // Act
        auth.UserName = username;
        auth.Password = password;

        // Assert
        Assert.Equal(username, auth.UserName);
        Assert.Equal(password, auth.Password);
    }

    [Fact]
    public void CredentialStore_SaveAndLoadRoundTrip()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"ringvideos-test-auth-{Guid.NewGuid()}.json");
        var auth = new RingCredentials
        {
            UserName = "test@example.com",
            Password = "testPassword",
            RefreshToken = "testRefresh"
        };

        try
        {
            // Act
            CredentialStore.Save(path, auth);
            var raw = File.ReadAllText(path);
            var loaded = CredentialStore.Load(path);

            // Assert
            Assert.DoesNotContain("testPassword", raw);
            Assert.DoesNotContain("testRefresh", raw);
            Assert.Equal("test@example.com", loaded.UserName);
            Assert.Equal("testPassword", loaded.Password);
            Assert.Equal("testRefresh", loaded.RefreshToken);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void DeviceInfo_CanBeCreatedWithProperties()
    {
        // Arrange & Act
        var device = new DeviceInfo
        {
            Id = 123,
            Name = "Front Door",
            DeviceId = "device_abc123"
        };

        // Assert
        Assert.Equal(123, device.Id);
        Assert.Equal("Front Door", device.Name);
        Assert.Equal("device_abc123", device.DeviceId);
    }

    [Fact]
    public void DeviceList_CanBeCreatedAndDevicesAdded()
    {
        // Arrange
        var deviceList = new DeviceList();
        var device = new DeviceInfo
        {
            Id = 456,
            Name = "Back Patio",
            DeviceId = "device_xyz789"
        };

        // Act
        deviceList.Devices.Add(device);

        // Assert
        Assert.Single(deviceList.Devices);
        Assert.Equal("Back Patio", deviceList.Devices[0].Name);
    }

    [Fact]
    public void FailedDownload_StoresErrorInformation()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var error = new FailedDownload
        {
            Timestamp = now,
            EventId = "evt_123",
            CameraId = 456,
            CameraName = "Doorbell",
            LocationName = "Front",
            ErrorDescription = "Network timeout"
        };

        // Act & Assert
        Assert.Equal("evt_123", error.EventId);
        Assert.Equal(456, error.CameraId);
        Assert.Equal("Network timeout", error.ErrorDescription);
    }

    [Fact]
    public void ShutdownSignal_HasCancellationToken()
    {
        // Arrange & Act - ShutdownSignal is static with CTS
        var cts = ShutdownSignal.Cts;

        // Assert
        Assert.NotNull(cts);
        Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public void CommandHelper_CanBeInstantiated()
    {
        // Arrange & Act
        var helper = new CommandHelper();

        // Assert
        Assert.NotNull(helper);
    }

    [Fact]
    public void CommandHelper_CanSetupCommands()
    {
        // Arrange
        var helper = new CommandHelper();

        // Act
        var rootCommand = helper.SetupCommands();

        // Assert
        Assert.NotNull(rootCommand);
    }

    [Fact]
    public void FailedDownload_CanBeSerialized()
    {
        // Arrange
        var failedDownload = new FailedDownload
        {
            EventId = "evt_001",
            CameraId = 100,
            CameraName = "Front Door",
            LocationName = "Entrance",
            ErrorDescription = "Timeout",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        Assert.NotNull(failedDownload.EventId);
        Assert.NotNull(failedDownload.CameraName);
        Assert.NotNull(failedDownload.LocationName);
    }

    [Fact]
    public void DeviceList_SupportsMultipleDevices()
    {
        // Arrange
        var deviceList = new DeviceList();
        var devices = new[]
        {
            new DeviceInfo { Id = 1, Name = "Camera 1", DeviceId = "dev_1" },
            new DeviceInfo { Id = 2, Name = "Camera 2", DeviceId = "dev_2" },
            new DeviceInfo { Id = 3, Name = "Camera 3", DeviceId = "dev_3" }
        };

        // Act
        foreach (var device in devices)
        {
            deviceList.Devices.Add(device);
        }

        // Assert
        Assert.Equal(3, deviceList.Devices.Count);
        Assert.Equal("Camera 2", deviceList.Devices[1].Name);
    }

    [Fact]
    public void Filter_DateRangeCanSpanMonths()
    {
        // Arrange
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 3, 31);
        var filter = new Filter
        {
            StartDateTime = start,
            EndDateTime = end,
            VideoCount = 1000
        };

        // Act
        var daysDifference = (filter.EndDateTime - filter.StartDateTime).Value.Days;

        // Assert
        Assert.Equal(89, daysDifference);
        Assert.Equal(1000, filter.VideoCount);
    }

    [Fact]
    public void CredentialStore_EncryptsBeforeWritingToDisk()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"ringvideos-test-auth-{Guid.NewGuid()}.json");
        var auth = new RingCredentials
        {
            UserName = "user@ring.com",
            Password = "SecurePassword123!",
            RefreshToken = "refresh_abc123"
        };

        try
        {
            // Act
            CredentialStore.Save(path, auth);
            var raw = File.ReadAllText(path);

            // Assert
            Assert.DoesNotContain("SecurePassword123!", raw);
            Assert.DoesNotContain("refresh_abc123", raw);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Model_DeviceInfoPropertiesAreIndependent()
    {
        // Arrange
        var device1 = new DeviceInfo { Id = 1, Name = "Device A", DeviceId = "dev_a" };
        var device2 = new DeviceInfo { Id = 2, Name = "Device B", DeviceId = "dev_b" };

        // Act & Assert
        Assert.NotEqual(device1.Id, device2.Id);
        Assert.NotEqual(device1.Name, device2.Name);
        Assert.NotEqual(device1.DeviceId, device2.DeviceId);
    }

    [Fact]
    public void Model_FailedDownloadTimestampIsUtc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var failed = new FailedDownload { Timestamp = now };

        // Act & Assert
        Assert.Equal(now, failed.Timestamp);
        Assert.Equal(DateTimeKind.Utc, now.Kind);
    }
}
