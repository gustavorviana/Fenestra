using Fenestra.Core.Models;

namespace Fenestra.Tests.Core.Models;

public class AppInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var version = new Version(2, 1, 0);
        var info = new AppInfo("TestApp", version);

        Assert.Equal("TestApp", info.AppName);
        Assert.Equal(version, info.Version);
    }
}
