using Fenestra.Windows;
using Fenestra.Wpf;
using Fenestra.Wpf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Windows.Wpf.Tests.Extensions;

public class WpfServiceCollectionExtensionsTests
{
    // --- AddWpfTrayIcon ---

    [Fact]
    public void AddWpfTrayIcon_RegistersTrayIconService()
    {
        var services = new ServiceCollection();

        services.AddWpfTrayIcon();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(ITrayIconService) &&
            d.ImplementationType == typeof(TrayIconService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWpfTrayIcon_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddWpfTrayIcon();

        Assert.Same(services, result);
    }

    // --- AddWpfMinimizeToTray ---

    [Fact]
    public void AddWpfMinimizeToTray_RegistersMinimizeToTrayService()
    {
        var services = new ServiceCollection();

        services.AddWpfMinimizeToTray();

        Assert.Contains(services, d => d.ServiceType == typeof(MinimizeToTrayService));
    }

    [Fact]
    public void AddWpfMinimizeToTray_AlsoRegistersTrayIcon()
    {
        var services = new ServiceCollection();

        services.AddWpfMinimizeToTray();

        Assert.Contains(services, d => d.ServiceType == typeof(ITrayIconService));
    }

    [Fact]
    public void AddWpfMinimizeToTray_WithOptionsAction_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection();

        services.AddWpfMinimizeToTray(opts =>
        {
            opts.AutoShowTrayIcon = false;
            opts.RestoreOnDoubleClick = false;
        });

        // Retrieve the registered singleton options instance from the descriptors
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MinimizeToTrayOptions));
        Assert.NotNull(descriptor);
        var registered = descriptor!.ImplementationInstance as MinimizeToTrayOptions;
        Assert.NotNull(registered);
        Assert.False(registered!.AutoShowTrayIcon);
        Assert.False(registered.RestoreOnDoubleClick);
    }

    [Fact]
    public void AddWpfMinimizeToTray_WithoutOptionsAction_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddWpfMinimizeToTray();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MinimizeToTrayOptions));
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor!.ImplementationInstance);
    }

    // --- AddWpfSingleInstance ---

    [Fact]
    public void AddWpfSingleInstance_RegistersSingleInstanceGuard()
    {
        var services = new ServiceCollection();

        services.AddWpfSingleInstance();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(SingleInstanceGuard) &&
            d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(Fenestra.Core.IFenestraModule) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // --- AddWpfGlobalHotkeys ---

    [Fact]
    public void AddWpfGlobalHotkeys_RegistersGlobalHotkeyService()
    {
        var services = new ServiceCollection();

        services.AddWpfGlobalHotkeys();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IGlobalHotkeyService) &&
            d.ImplementationType == typeof(GlobalHotkeyService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // --- Fluent chaining across all extensions ---

    [Fact]
    public void AllExtensions_ChainFluentlyWithoutThrowing()
    {
        var services = new ServiceCollection();

        var ex = Record.Exception(() => services
            .AddWpfTrayIcon()
            .AddWpfMinimizeToTray()
            .AddWpfSingleInstance()
            .AddWpfGlobalHotkeys());

        Assert.Null(ex);
    }
}
