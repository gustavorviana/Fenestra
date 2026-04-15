using Fenestra.Core;
using Fenestra.Windows.Services;
using NSubstitute;
using System.Globalization;

namespace Fenestra.Windows.Tests.Services;

/// <summary>
/// Tests mutate global <see cref="CultureInfo.DefaultThreadCurrent*"/> state. The test
/// class saves and restores the original culture in <see cref="Dispose"/> to avoid
/// leaking into other tests.
/// </summary>
public class LocalizationServiceTests : IDisposable
{
    private const string SectionName = "Localization";
    private const string KeySelectedCulture = "SelectedCulture";

    private readonly CultureInfo? _savedDefaultCulture;
    private readonly CultureInfo? _savedDefaultUiCulture;
    private readonly CultureInfo _savedThreadCulture;
    private readonly CultureInfo _savedThreadUiCulture;

    private readonly IRegistryConfig _rootConfig = Substitute.For<IRegistryConfig>();
    private readonly IRegistryConfig _section = Substitute.For<IRegistryConfig>();

    public LocalizationServiceTests()
    {
        _savedDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        _savedDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        _savedThreadCulture = Thread.CurrentThread.CurrentCulture;
        _savedThreadUiCulture = Thread.CurrentThread.CurrentUICulture;

        _rootConfig.GetSection(SectionName, createIfNotExists: true).Returns(_section);
    }

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _savedDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _savedDefaultUiCulture;
        Thread.CurrentThread.CurrentCulture = _savedThreadCulture;
        Thread.CurrentThread.CurrentUICulture = _savedThreadUiCulture;
    }

    private LocalizationService CreateSut(
        string[]? supported = null,
        string? @default = null,
        string? persisted = null,
        bool runInit = true)
    {
        var opts = new LocalizationOptions
        {
            Supported = supported ?? new[] { "en-US", "pt-BR", "es-ES" },
            Default = @default ?? "en-US",
        };
        SetupPersisted(persisted);
        var sut = new LocalizationService(opts, _rootConfig);
        if (runInit)
            sut.InitAsync(CancellationToken.None).GetAwaiter().GetResult();
        return sut;
    }

    private void SetupPersisted(string? persisted)
    {
        _section.TryGet<string>(KeySelectedCulture, out Arg.Any<string?>())
            .Returns(ci => { ci[1] = persisted; return persisted is not null; });
    }

    // =====================================================================
    // Ctor — options validation
    // =====================================================================

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LocalizationService(null!, _rootConfig));
    }

    [Fact]
    public void Ctor_NullConfig_Throws()
    {
        var opts = new LocalizationOptions { Supported = new[] { "en-US" }, Default = "en-US" };
        Assert.Throws<ArgumentNullException>(() => new LocalizationService(opts, null!));
    }

    [Fact]
    public void Ctor_EmptySupported_Throws()
    {
        var opts = new LocalizationOptions { Supported = Array.Empty<string>(), Default = "en-US" };
        Assert.Throws<ArgumentException>(() => new LocalizationService(opts, _rootConfig));
    }

    [Fact]
    public void Ctor_InvalidCultureName_Throws()
    {
        var opts = new LocalizationOptions
        {
            Supported = new[] { "not-a-real-culture-zzzz" },
            Default = "not-a-real-culture-zzzz",
        };
        Assert.Throws<ArgumentException>(() => new LocalizationService(opts, _rootConfig));
    }

    [Fact]
    public void Ctor_EmptyDefault_Throws()
    {
        var opts = new LocalizationOptions { Supported = new[] { "en-US" }, Default = "" };
        Assert.Throws<ArgumentException>(() => new LocalizationService(opts, _rootConfig));
    }

    [Fact]
    public void Ctor_DefaultNotInSupported_Throws()
    {
        var opts = new LocalizationOptions
        {
            Supported = new[] { "en-US", "pt-BR" },
            Default = "fr-FR",
        };
        Assert.Throws<ArgumentException>(() => new LocalizationService(opts, _rootConfig));
    }

    [Fact]
    public void Ctor_ValidOptions_DoesNotThrow()
    {
        SetupPersisted(null);
        var ex = Record.Exception(() => CreateSut());
        Assert.Null(ex);
    }

    // =====================================================================
    // Ctor — pre-InitAsync state
    // =====================================================================

    [Fact]
    public void Ctor_BeforeInit_CurrentCultureSeededWithDefault()
    {
        SetupPersisted("pt-BR");
        var sut = CreateSut(
            supported: new[] { "en-US", "pt-BR" },
            @default: "en-US",
            persisted: "pt-BR",
            runInit: false);

        // Before InitAsync runs, CurrentCulture is seeded with the configured default,
        // not the persisted culture.
        Assert.Equal("en-US", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_BeforeInit_DoesNotTouchProcessCulture()
    {
        var before = Thread.CurrentThread.CurrentCulture;
        SetupPersisted("pt-BR");

        _ = CreateSut(
            supported: new[] { "en-US", "pt-BR" },
            @default: "en-US",
            persisted: "pt-BR",
            runInit: false);

        Assert.Equal(before.Name, Thread.CurrentThread.CurrentCulture.Name);
    }

    // =====================================================================
    // InitAsync — IFenestraModule contract
    // =====================================================================

    [Fact]
    public void LocalizationService_ImplementsIFenestraModule()
    {
        var sut = CreateSut(runInit: false);
        Assert.IsAssignableFrom<IFenestraModule>(sut);
    }

    [Fact]
    public async Task InitAsync_ResolvesPersistedCulture()
    {
        SetupPersisted("pt-BR");
        var sut = CreateSut(
            supported: new[] { "en-US", "pt-BR" },
            @default: "en-US",
            persisted: "pt-BR",
            runInit: false);

        await ((IFenestraModule)sut).InitAsync(CancellationToken.None);

        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
    }

    // =====================================================================
    // Ctor + InitAsync — initial culture resolution
    // =====================================================================

    [Fact]
    public void Ctor_PersistedCultureInSupported_UsesPersisted()
    {
        var sut = CreateSut(persisted: "pt-BR");
        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_PersistedCultureCaseInsensitive_NormalizedToCanonicalName()
    {
        var sut = CreateSut(persisted: "PT-br");
        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_PersistedCultureNotInSupported_FallsThroughToOsOrDefault()
    {
        // Persisted "ja-JP" isn't supported; fallback path kicks in.
        // To make the test deterministic, force the "current" UI culture to something
        // also not in Supported so the fallback chain reaches Default.
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");

        var sut = CreateSut(
            supported: new[] { "en-US", "es-ES" },
            @default: "en-US",
            persisted: "ja-JP");

        Assert.Equal("en-US", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_NoPersisted_OsInSupported_UsesOs()
    {
        // Force "OS" culture (via DefaultThreadCurrentUICulture) to pt-BR, which IS supported.
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");

        var sut = CreateSut(
            supported: new[] { "en-US", "pt-BR" },
            @default: "en-US",
            persisted: null);

        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_NoPersisted_OsNotInSupported_UsesDefault()
    {
        // Force "OS" culture to ja-JP, which is NOT in supported.
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");

        var sut = CreateSut(
            supported: new[] { "en-US", "pt-BR" },
            @default: "en-US",
            persisted: null);

        Assert.Equal("en-US", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_NoPersisted_OsLanguageOnlyFallback_Matches()
    {
        // OS is en-GB (not in supported), but "en" 2-letter language matches en-US (by language).
        // The fallback looks for FindSupported("en") which only matches a culture named "en"
        // exactly. If Supported has "en" as a neutral culture, it matches.
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

        var sut = CreateSut(
            supported: new[] { "en", "pt-BR" }, // neutral "en" included
            @default: "pt-BR",
            persisted: null);

        Assert.Equal("en", sut.CurrentCulture.Name);
    }

    [Fact]
    public void Ctor_AppliesCultureToCurrentThread()
    {
        var sut = CreateSut(persisted: "pt-BR");

        Assert.Equal("pt-BR", Thread.CurrentThread.CurrentCulture.Name);
        Assert.Equal("pt-BR", Thread.CurrentThread.CurrentUICulture.Name);
    }

    [Fact]
    public void Ctor_AppliesCultureToDefaultThread()
    {
        var sut = CreateSut(persisted: "es-ES");

        Assert.Equal("es-ES", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("es-ES", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public void Ctor_DoesNotRaiseCultureChangedOnInitialLoad()
    {
        // Event subscription is only possible AFTER construction, so this test verifies
        // that subscribing between ctor and first SetCulture call sees zero events.
        var sut = CreateSut(persisted: "pt-BR");
        var raised = 0;
        sut.CultureChanged += (_, _) => raised++;

        // No SetCulture called yet — subscriber should see nothing.
        Assert.Equal(0, raised);
    }

    // =====================================================================
    // SetCulture — validation
    // =====================================================================

    [Fact]
    public void SetCulture_Null_Throws()
    {
        var sut = CreateSut();
        Assert.Throws<ArgumentNullException>(() => sut.SetCulture(null!));
    }

    [Fact]
    public void SetCulture_UnsupportedCulture_Throws()
    {
        var sut = CreateSut(supported: new[] { "en-US", "pt-BR" }, @default: "en-US");
        Assert.Throws<ArgumentException>(
            () => sut.SetCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void SetCulture_ExceptionMessageMentionsSupportedCultures()
    {
        var sut = CreateSut(supported: new[] { "en-US", "pt-BR" }, @default: "en-US");

        var ex = Assert.Throws<ArgumentException>(
            () => sut.SetCulture(CultureInfo.GetCultureInfo("fr-FR")));

        Assert.Contains("en-US", ex.Message);
        Assert.Contains("pt-BR", ex.Message);
    }

    // =====================================================================
    // SetCulture — happy path
    // =====================================================================

    [Fact]
    public void SetCulture_SupportedCulture_UpdatesCurrentCulture()
    {
        var sut = CreateSut(persisted: "en-US");

        sut.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));

        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
    }

    [Fact]
    public void SetCulture_SupportedCulture_PersistsToRegistry()
    {
        var sut = CreateSut(persisted: "en-US");

        sut.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));

        _section.Received().Set(KeySelectedCulture, "pt-BR");
    }

    [Fact]
    public void SetCulture_SupportedCulture_AppliesToCurrentThread()
    {
        var sut = CreateSut(persisted: "en-US");

        sut.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));

        Assert.Equal("pt-BR", Thread.CurrentThread.CurrentUICulture.Name);
        Assert.Equal("pt-BR", Thread.CurrentThread.CurrentCulture.Name);
    }

    [Fact]
    public void SetCulture_SupportedCulture_AppliesToDefaultThread()
    {
        var sut = CreateSut(persisted: "en-US");

        sut.SetCulture(CultureInfo.GetCultureInfo("es-ES"));

        Assert.Equal("es-ES", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("es-ES", CultureInfo.DefaultThreadCurrentCulture?.Name);
    }

    [Fact]
    public void SetCulture_SupportedCulture_RaisesCultureChangedWithOldAndNew()
    {
        var sut = CreateSut(persisted: "en-US");
        CultureChangedEventArgs? received = null;
        sut.CultureChanged += (_, e) => received = e;

        sut.SetCulture(CultureInfo.GetCultureInfo("pt-BR"));

        Assert.NotNull(received);
        Assert.Equal("en-US", received!.OldCulture.Name);
        Assert.Equal("pt-BR", received.NewCulture.Name);
    }

    [Fact]
    public void SetCulture_SameAsCurrent_IsNoOp()
    {
        var sut = CreateSut(persisted: "en-US");
        _section.ClearReceivedCalls();
        var raised = 0;
        sut.CultureChanged += (_, _) => raised++;

        sut.SetCulture(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal(0, raised);
        _section.DidNotReceive().Set(KeySelectedCulture, Arg.Any<object?>());
    }

    [Fact]
    public void SetCulture_CaseInsensitive_NormalizesAndAccepts()
    {
        var sut = CreateSut(persisted: "en-US");

        // Try to pass "PT-br" — CultureInfo.GetCultureInfo normalizes to "pt-BR".
        sut.SetCulture(CultureInfo.GetCultureInfo("PT-br"));

        Assert.Equal("pt-BR", sut.CurrentCulture.Name);
        _section.Received().Set(KeySelectedCulture, "pt-BR");
    }

    // =====================================================================
    // SupportedCultures property
    // =====================================================================

    [Fact]
    public void SupportedCultures_ReflectsOptions()
    {
        var sut = CreateSut(
            supported: new[] { "en-US", "pt-BR", "es-ES" },
            @default: "en-US");

        var names = sut.SupportedCultures.Select(c => c.Name).ToArray();
        Assert.Equal(new[] { "en-US", "pt-BR", "es-ES" }, names);
    }
}
