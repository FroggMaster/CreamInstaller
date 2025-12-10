using CreamInstaller.Utility;

namespace CreamInstaller.Tests;

public class HttpClientManagerTests
{
    [Fact]
    public void DefaultTimeoutSeconds_HasReasonableValue()
    {
        // Should be between 10 seconds and 2 minutes
        Assert.True(HttpClientManager.DefaultTimeoutSeconds >= 10);
        Assert.True(HttpClientManager.DefaultTimeoutSeconds <= 120);
    }

    [Fact]
    public void ExtendedTimeoutSeconds_IsGreaterThanDefault()
    {
        Assert.True(HttpClientManager.ExtendedTimeoutSeconds > HttpClientManager.DefaultTimeoutSeconds);
    }

    [Fact]
    public void ExtendedTimeoutSeconds_HasReasonableValue()
    {
        // Should be between 1 minute and 10 minutes
        Assert.True(HttpClientManager.ExtendedTimeoutSeconds >= 60);
        Assert.True(HttpClientManager.ExtendedTimeoutSeconds <= 600);
    }
}
