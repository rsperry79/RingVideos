using KoenZomers.Ring.Api;

namespace RingVideos.Tests;

public class WorkerAuthResolutionTests
{
    [Fact]
    public void RefreshToken_Present_SucceedsWithoutUsernameOrPassword()
    {
        var auth = new RingCredentials { RefreshToken = "cached-refresh-token" };

        var error = Worker.ResolveAuthError(auth);

        Assert.Null(error);
    }

    [Fact]
    public void UsernameAndPassword_Present_Succeeds()
    {
        var auth = new RingCredentials { UserName = "user@example.com", Password = "pw" };

        var error = Worker.ResolveAuthError(auth);

        Assert.Null(error);
    }

    [Fact]
    public void NoCredentialsAnywhere_FailsWithUsernameError()
    {
        var auth = new RingCredentials();

        var error = Worker.ResolveAuthError(auth);

        Assert.Equal("A Ring username is required", error);
    }

    [Fact]
    public void UsernameOnly_NoPassword_FailsWithPasswordError()
    {
        var auth = new RingCredentials { UserName = "user@example.com" };

        var error = Worker.ResolveAuthError(auth);

        Assert.Equal("A Ring password is required", error);
    }

    [Fact]
    public void RefreshToken_TakesPriorityOverIncompleteUsernamePassword()
    {
        // A username with no password would normally fail, but a refresh token short-circuits
        // that check entirely - this is the bug this test guards against regressing.
        var auth = new RingCredentials { RefreshToken = "cached-refresh-token", UserName = "user@example.com" };

        var error = Worker.ResolveAuthError(auth);

        Assert.Null(error);
    }
}
