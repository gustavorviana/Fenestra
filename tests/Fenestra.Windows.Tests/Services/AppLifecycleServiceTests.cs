using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Services;
using NSubstitute;

namespace Fenestra.Windows.Tests.Services;

public class AppLifecycleServiceTests
{
    private const string SectionName = "Lifecycle";
    private const string KeyFirstInstallDate = "FirstInstallDate";
    private const string KeyLastVersion = "LastVersion";
    private const string KeyLaunchCount = "LaunchCount";

    private readonly AppInfo _appInfo = new("Test App", "TestApp", new Version(2, 0));
    private readonly IRegistryConfig _rootConfig = Substitute.For<IRegistryConfig>();
    private readonly IRegistryConfig _section = Substitute.For<IRegistryConfig>();

    public AppLifecycleServiceTests()
    {
        _rootConfig.GetSection(SectionName, createIfNotExists: true).Returns(_section);
    }

    private AppLifecycleService CreateSut() => new(_appInfo, _rootConfig);

    /// <summary>
    /// Fresh registry — all TryGet/Get calls return default/false.
    /// This is NSubstitute's default behaviour with no explicit setup, but we make it
    /// explicit for documentation.
    /// </summary>
    private void SetupFreshRegistry()
    {
        _section.TryGet<DateTimeOffset>(KeyFirstInstallDate, out Arg.Any<DateTimeOffset>())
            .Returns(ci => { ci[1] = default(DateTimeOffset); return false; });
        _section.TryGet<Version>(KeyLastVersion, out Arg.Any<Version?>())
            .Returns(ci => { ci[1] = null; return false; });
        _section.Get<int>(KeyLaunchCount).Returns(0);
    }

    private void SetupExistingState(DateTimeOffset installDate, Version lastVersion, int launchCount)
    {
        _section.TryGet<DateTimeOffset>(KeyFirstInstallDate, out Arg.Any<DateTimeOffset>())
            .Returns(ci => { ci[1] = installDate; return true; });
        _section.TryGet<Version>(KeyLastVersion, out Arg.Any<Version?>())
            .Returns(ci => { ci[1] = lastVersion; return true; });
        _section.Get<int>(KeyLaunchCount).Returns(launchCount);
    }

    // =====================================================================
    // Constructor validation
    // =====================================================================

    [Fact]
    public void Ctor_NullAppInfo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AppLifecycleService(null!, _rootConfig));
    }

    [Fact]
    public void Ctor_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AppLifecycleService(_appInfo, null!));
    }

    // =====================================================================
    // First run (empty registry)
    // =====================================================================

    [Fact]
    public void FirstRun_IsFirstRunTrue()
    {
        SetupFreshRegistry();
        var sut = CreateSut();
        Assert.True(sut.IsFirstRun);
    }

    [Fact]
    public void FirstRun_IsFirstRunOfVersionTrue()
    {
        SetupFreshRegistry();
        var sut = CreateSut();
        Assert.True(sut.IsFirstRunOfVersion);
    }

    [Fact]
    public void FirstRun_PreviousVersionNull()
    {
        SetupFreshRegistry();
        var sut = CreateSut();
        Assert.Null(sut.PreviousVersion);
    }

    [Fact]
    public void FirstRun_LaunchCountIsOne()
    {
        SetupFreshRegistry();
        var sut = CreateSut();
        Assert.Equal(1, sut.LaunchCount);
    }

    [Fact]
    public void FirstRun_FirstInstallDateIsApproximatelyNow()
    {
        SetupFreshRegistry();
        var before = DateTimeOffset.UtcNow;

        var sut = CreateSut();

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(sut.FirstInstallDate, before, after);
    }

    [Fact]
    public void FirstRun_PersistsFirstInstallDateAsDateTimeOffset()
    {
        SetupFreshRegistry();

        CreateSut();

        // Service passes the DateTimeOffset directly; the RegistryConfigService
        // is responsible for converting to ISO 8601 string internally.
        _section.Received(1).Set(KeyFirstInstallDate, Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void FirstRun_PersistsLastVersionAsVersion()
    {
        SetupFreshRegistry();

        CreateSut();

        _section.Received(1).Set(KeyLastVersion, _appInfo.Version);
    }

    [Fact]
    public void FirstRun_PersistsLaunchCount1()
    {
        SetupFreshRegistry();

        CreateSut();

        _section.Received(1).Set(KeyLaunchCount, 1);
    }

    // =====================================================================
    // Second run — same version
    // =====================================================================

    [Fact]
    public void SecondRun_IsFirstRunFalse()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(2, 0), launchCount: 1);
        var sut = CreateSut();
        Assert.False(sut.IsFirstRun);
    }

    [Fact]
    public void SecondRun_IsFirstRunOfVersionFalse()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(2, 0), launchCount: 1);
        var sut = CreateSut();
        Assert.False(sut.IsFirstRunOfVersion);
    }

    [Fact]
    public void SecondRun_PreviousVersionNull()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(2, 0), launchCount: 1);
        var sut = CreateSut();
        Assert.Null(sut.PreviousVersion);
    }

    [Fact]
    public void SecondRun_LaunchCountIncrementsTo2()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(2, 0), launchCount: 1);
        var sut = CreateSut();
        Assert.Equal(2, sut.LaunchCount);
    }

    [Fact]
    public void SecondRun_FirstInstallDatePreserved()
    {
        var original = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        SetupExistingState(original, new Version(2, 0), launchCount: 1);

        var sut = CreateSut();

        Assert.Equal(original, sut.FirstInstallDate);
    }

    [Fact]
    public void SecondRun_DoesNotOverwriteFirstInstallDate()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(2, 0), launchCount: 1);

        CreateSut();

        _section.DidNotReceive().Set(KeyFirstInstallDate, Arg.Any<object?>());
    }

    // =====================================================================
    // Upgrade — new version
    // =====================================================================

    [Fact]
    public void Upgrade_IsFirstRunFalse()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);
        var sut = CreateSut();
        Assert.False(sut.IsFirstRun);
    }

    [Fact]
    public void Upgrade_IsFirstRunOfVersionTrue()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);
        var sut = CreateSut();
        Assert.True(sut.IsFirstRunOfVersion);
    }

    [Fact]
    public void Upgrade_PreviousVersionIsOldVersion()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);
        var sut = CreateSut();
        Assert.Equal(new Version(1, 0), sut.PreviousVersion);
    }

    [Fact]
    public void Upgrade_LaunchCountIncrements()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);
        var sut = CreateSut();
        Assert.Equal(43, sut.LaunchCount);
    }

    [Fact]
    public void Upgrade_PersistsNewVersion()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);

        CreateSut();

        _section.Received(1).Set(KeyLastVersion, new Version(2, 0));
    }

    [Fact]
    public void Upgrade_DoesNotOverwriteFirstInstallDate()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-30), new Version(1, 0), launchCount: 42);

        CreateSut();

        _section.DidNotReceive().Set(KeyFirstInstallDate, Arg.Any<object?>());
    }

    // =====================================================================
    // Downgrade (treated as a version change — same as upgrade)
    // =====================================================================

    [Fact]
    public void Downgrade_IsFirstRunOfVersionTrue()
    {
        SetupExistingState(DateTimeOffset.UtcNow.AddDays(-1), new Version(3, 0), launchCount: 5);
        var sut = CreateSut();

        // current version is 2.0, stored was 3.0 → version change
        Assert.True(sut.IsFirstRunOfVersion);
        Assert.Equal(new Version(3, 0), sut.PreviousVersion);
    }

    // =====================================================================
    // Corrupted registry — defensive parsing via TryGet returning false
    // =====================================================================

    [Fact]
    public void CorruptedInstallDate_TreatedAsFirstRun()
    {
        // Simulate TryGet<DateTimeOffset> returning false because the stored value
        // can't be converted (the real RegistryConfigService catches the parse
        // exception inside TryGet and returns false).
        _section.TryGet<DateTimeOffset>(KeyFirstInstallDate, out Arg.Any<DateTimeOffset>())
            .Returns(ci => { ci[1] = default(DateTimeOffset); return false; });
        _section.TryGet<Version>(KeyLastVersion, out Arg.Any<Version?>())
            .Returns(ci => { ci[1] = new Version(2, 0); return true; });
        _section.Get<int>(KeyLaunchCount).Returns(5);

        var sut = CreateSut();

        Assert.True(sut.IsFirstRun);
    }

    [Fact]
    public void CorruptedLastVersion_TreatedAsFirstRunOfVersion()
    {
        var installed = DateTimeOffset.UtcNow.AddDays(-10);
        _section.TryGet<DateTimeOffset>(KeyFirstInstallDate, out Arg.Any<DateTimeOffset>())
            .Returns(ci => { ci[1] = installed; return true; });
        _section.TryGet<Version>(KeyLastVersion, out Arg.Any<Version?>())
            .Returns(ci => { ci[1] = null; return false; });
        _section.Get<int>(KeyLaunchCount).Returns(5);

        var sut = CreateSut();

        // IsFirstRun is false (install date was valid), but version comparison fails → FirstRunOfVersion
        Assert.False(sut.IsFirstRun);
        Assert.True(sut.IsFirstRunOfVersion);
        // PreviousVersion null because the stored value couldn't be parsed
        Assert.Null(sut.PreviousVersion);
    }

    // =====================================================================
    // Disposal — section IDisposable via `using`
    // =====================================================================

    [Fact]
    public void Ctor_DisposesRegistrySection()
    {
        SetupFreshRegistry();

        CreateSut();

        _section.Received(1).Dispose();
    }
}
