using Fenestra.Core;
using Fenestra.Windows.Extensions;
using Fenestra.Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Windows.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    // --- AddWindowsToastNotifications ---

    [Fact]
    public void AddWindowsToastNotifications_RegistersToastService()
    {
        var services = new ServiceCollection();

        services.AddWindowsToastNotifications();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IToastService) &&
            d.ImplementationType == typeof(ToastService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWindowsToastNotifications_RegistersNotificationRegistrationManager()
    {
        var services = new ServiceCollection();

        services.AddWindowsToastNotifications();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IAumidRegistrationManager) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWindowsToastNotifications_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddWindowsToastNotifications();

        Assert.Same(services, result);
    }

    // --- AddWindowsJumpList ---

    [Fact]
    public void AddWindowsJumpList_RegistersJumpListService()
    {
        var services = new ServiceCollection();

        services.AddWindowsJumpList();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IJumpListService) &&
            d.ImplementationType == typeof(JumpListService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWindowsJumpList_TransitivelyRegistersAumidRegistrationManager()
    {
        var services = new ServiceCollection();

        services.AddWindowsJumpList();

        Assert.Contains(services, d => d.ServiceType == typeof(IAumidRegistrationManager));
    }

    [Fact]
    public void AddWindowsJumpList_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddWindowsJumpList();

        Assert.Same(services, result);
    }

    // --- AddWindowsTaskbarOverlay ---

    [Fact]
    public void AddWindowsTaskbarOverlay_RegistersTaskbarOverlayService()
    {
        var services = new ServiceCollection();

        services.AddWindowsTaskbarOverlay();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(ITaskbarOverlayService) &&
            d.ImplementationType == typeof(TaskbarOverlayService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWindowsTaskbarOverlay_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddWindowsTaskbarOverlay();

        Assert.Same(services, result);
    }

    // --- AddWindowsToastActivation ---

    [Fact]
    public void AddWindowsToastActivation_RegistersActivationRegistrar()
    {
        var services = new ServiceCollection();

        services.AddWindowsToastActivation();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(ToastActivationRegistrar) &&
            d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IToastActivationRegistrar) &&
            d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(Fenestra.Core.IFenestraModule) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // --- AddWindowsAutoStart ---

    [Fact]
    public void AddWindowsAutoStart_RegistersAutoStartService()
    {
        var services = new ServiceCollection();

        services.AddWindowsAutoStart();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IAutoStartService) &&
            d.ImplementationType == typeof(AutoStartService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // --- AddWindowsThemeDetection ---

    [Fact]
    public void AddWindowsThemeDetection_RegistersThemeService()
    {
        var services = new ServiceCollection();

        services.AddWindowsThemeDetection();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IThemeService) &&
            d.ImplementationType == typeof(ThemeService) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    // --- All extensions return IServiceCollection for chaining ---

    [Fact]
    public void AllExtensions_ChainFluentlyWithoutThrowing()
    {
        var services = new ServiceCollection();

        var ex = Record.Exception(() => services
            .AddWindowsToastNotifications()
            .AddWindowsToastActivation()
            .AddWindowsAutoStart()
            .AddWindowsThemeDetection());

        Assert.Null(ex);
    }
}
