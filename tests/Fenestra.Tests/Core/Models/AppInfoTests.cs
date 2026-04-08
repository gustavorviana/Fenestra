using Fenestra.Core.Models;
using Fenestra.Windows.Models;

namespace Fenestra.Tests.Core.Models;

public class AppInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var version = new Version(2, 1, 0);
        var info = new AppInfo("TestApp", version);

        Assert.Equal("TestApp", info.AppName);
        Assert.Equal("TestApp", info.AppId);
        Assert.Equal(version, info.Version);
    }

    [Fact]
    public void Constructor_FiltersAppId_ToAlphanumeric()
    {
        var info = new AppInfo("My App! 2.0", new Version(1, 0));

        Assert.Equal("MyApp20", info.AppId);
    }

    [Fact]
    public void PackagedConstructor_SetsPackageProperties()
    {
        var version = new Version(3, 2, 1);
        var info = new WindowsAppInfo("Calculator", "Microsoft.Calculator_8wekyb!App", version, "Microsoft.Calculator_8wekyb");

        Assert.Equal("Calculator", info.AppName);
        Assert.Equal("Microsoft.Calculator_8wekyb!App", info.AppId);
        Assert.Equal(version, info.Version);
        Assert.True(info.IsPackagedApp);
        Assert.Equal("Microsoft.Calculator_8wekyb", info.PackageFamilyName);
    }

    [Fact]
    public void CustomAppIdConstructor_SetsExplicitAppId()
    {
        var version = new Version(1, 0);
        var info = new AppInfo("My App", "com.example.myapp", version);

        Assert.Equal("My App", info.AppName);
        Assert.Equal("com.example.myapp", info.AppId);
        Assert.Equal(version, info.Version);
    }

    [Fact]
    public void CustomAppIdConstructor_ThrowsOnEmptyAppId()
    {
        Assert.Throws<ArgumentException>(() =>
            new AppInfo("App", "", new Version(1, 0)));
    }

    [Fact]
    public void Constructor_AssemblyFallback_DerivesAppIdFromName()
    {
        var assemblyName = "Fenestra.Sample.BuilderStyle";
        var assemblyVersion = new Version(1, 0, 0);
        var info = new AppInfo(assemblyName, assemblyVersion);

        Assert.Equal(assemblyName, info.AppName);
        Assert.Equal("FenestraSampleBuilderStyle", info.AppId);
        Assert.Equal(assemblyVersion, info.Version);
    }

    [Fact]
    public void PackagedConstructor_ThrowsOnEmptyAumid()
    {
        Assert.Throws<ArgumentException>(() =>
            new WindowsAppInfo("App", "", new Version(1, 0), "Family"));
    }

    [Fact]
    public void PackagedConstructor_ThrowsOnEmptyFamilyName()
    {
        Assert.Throws<ArgumentException>(() =>
            new WindowsAppInfo("App", "aumid", new Version(1, 0), ""));
    }
}
