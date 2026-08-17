using RingVideos.Models;
using System.Text;

namespace RingVideos.Tests;

public class AuthenticationTests
{
    [Fact]
    public void Authentication_DefaultsAreNull()
    {
        var auth = new Authentication();
        Assert.Null(auth.UserName);
        Assert.Null(auth.Password);
        Assert.Null(auth.RefreshToken);
    }

    [Fact]
    public void Authentication_CanSetClearTextPassword()
    {
        var auth = new Authentication { ClearTextPassword = "secret123" };
        Assert.Equal("secret123", auth.ClearTextPassword);
    }

    [Fact]
    public void Authentication_CanSetClearTextRefreshToken()
    {
        var auth = new Authentication { ClearTextRefreshToken = "refresh_token_abc" };
        Assert.Equal("refresh_token_abc", auth.ClearTextRefreshToken);
    }

    [Fact]
    public void Authentication_EncryptPassword()
    {
        var auth = new Authentication
        {
            UserName = "user@example.com",
            ClearTextPassword = "plaintext_password"
        };

        auth.Encrypt();

        Assert.NotNull(auth.Password);
        Assert.NotEqual("plaintext_password", auth.Password);
    }

    [Fact]
    public void Authentication_DecryptPassword()
    {
        var auth = new Authentication
        {
            UserName = "user@example.com",
            ClearTextPassword = "plaintext_password"
        };

        auth.Encrypt();
        var encryptedPassword = auth.Password;

        auth.Decrypt();

        Assert.NotNull(auth.ClearTextPassword);
        Assert.Equal("plaintext_password", auth.ClearTextPassword);
    }

    [Fact]
    public void Authentication_EncryptAndDecryptRefreshToken()
    {
        var auth = new Authentication
        {
            UserName = "user@example.com",
            ClearTextRefreshToken = "refresh_token_123"
        };

        auth.Encrypt();
        auth.Decrypt();

        Assert.Equal("refresh_token_123", auth.ClearTextRefreshToken);
    }

    [Fact]
    public void Authentication_MultipleEncryptDecryptCycles()
    {
        var auth = new Authentication
        {
            UserName = "test@test.com",
            ClearTextPassword = "password_v1",
            ClearTextRefreshToken = "refresh_v1"
        };

        // First cycle
        auth.Encrypt();
        var encrypted1 = auth.Password;
        auth.Decrypt();
        Assert.Equal("password_v1", auth.ClearTextPassword);

        // Second cycle
        auth.ClearTextPassword = "password_v2";
        auth.Encrypt();
        var encrypted2 = auth.Password;
        Assert.NotEqual(encrypted1, encrypted2);
        auth.Decrypt();
        Assert.Equal("password_v2", auth.ClearTextPassword);
    }

    [Fact]
    public void Authentication_PreservesUserNameDuringEncryption()
    {
        var username = "preserved@example.com";
        var auth = new Authentication
        {
            UserName = username,
            ClearTextPassword = "pass"
        };

        auth.Encrypt();
        Assert.Equal(username, auth.UserName);

        auth.Decrypt();
        Assert.Equal(username, auth.UserName);
    }

    [Fact]
    public void Authentication_EmptyPasswordHandling()
    {
        var auth = new Authentication
        {
            UserName = "user@example.com",
            ClearTextPassword = ""
        };

        auth.Encrypt();
        auth.Decrypt();

        Assert.Equal("", auth.ClearTextPassword);
    }

    [Fact]
    public void Authentication_SpecialCharactersInPassword()
    {
        var specialPassword = "P@ssw0rd!#$%^&*()_+-=[]{}|;':\",./<>?";
        var auth = new Authentication
        {
            UserName = "user@example.com",
            ClearTextPassword = specialPassword
        };

        auth.Encrypt();
        var encrypted = auth.Password;
        Assert.NotEqual(specialPassword, encrypted);

        auth.Decrypt();
        Assert.Equal(specialPassword, auth.ClearTextPassword);
    }
}
