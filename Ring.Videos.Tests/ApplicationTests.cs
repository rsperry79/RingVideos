using Ring.Videos;

namespace Ring.Videos.Tests;

public class ApplicationTests
{
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
}
