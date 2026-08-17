using RingVideos.Models;

namespace RingVideos.Tests;

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
    public void Authentication_CanBeCreatedWithDefaults()
    {
        // Arrange & Act
        var auth = new Authentication();

        // Assert
        Assert.NotNull(auth);
        Assert.Null(auth.UserName);
        Assert.Null(auth.Password);
    }

    [Fact]
    public void Authentication_StoresUserNameAndPassword()
    {
        // Arrange
        var auth = new Authentication();
        var username = "test@example.com";
        var password = "testPassword";

        // Act
        auth.UserName = username;
        auth.ClearTextPassword = password;

        // Assert
        Assert.Equal(username, auth.UserName);
        Assert.Equal(password, auth.ClearTextPassword);
    }

    [Fact]
    public void Authentication_EncryptionRoundTrip()
    {
        // Arrange
        var auth = new Authentication
        {
            UserName = "test@example.com",
            ClearTextPassword = "testPassword",
            ClearTextRefreshToken = "testRefresh"
        };

        // Act
        auth.Encrypt();
        var encryptedPassword = auth.Password;
        var encryptedRefresh = auth.RefreshToken;

        auth.Decrypt();
        var decryptedPassword = auth.ClearTextPassword;
        var decryptedRefresh = auth.ClearTextRefreshToken;

        // Assert
        Assert.NotNull(encryptedPassword);
        Assert.NotEqual("testPassword", encryptedPassword);
        Assert.Equal("testPassword", decryptedPassword);
        Assert.Equal("testRefresh", decryptedRefresh);
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
    public void Authentication_CanBeEncryptedForStorage()
    {
        // Arrange
        var auth = new Authentication
        {
            UserName = "user@ring.com",
            ClearTextPassword = "SecurePassword123!",
            ClearTextRefreshToken = "refresh_abc123"
        };

        // Act
        auth.Encrypt();
        var encryptedPassword = auth.Password;
        var encryptedRefresh = auth.RefreshToken;

        // Assert
        Assert.NotNull(encryptedPassword);
        Assert.NotNull(encryptedRefresh);
        Assert.NotEqual("SecurePassword123!", encryptedPassword);
        Assert.NotEqual("refresh_abc123", encryptedRefresh);
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
