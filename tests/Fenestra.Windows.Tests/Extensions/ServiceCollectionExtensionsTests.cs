using Fenestra.Windows;
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
            d.ServiceType == typeof(IWindowsNotificationRegistrationManager) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddWindowsToastNotifications_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddWindowsToastNotifications();

        Assert.Same(services, result);
    }

    // --- AddWindowsToastActivation ---

    [Fact]
    public void AddWindowsToastActivation_RegistersActivationRegistrar()
    {
        var services = new ServiceCollection();

        services.AddWindowsToastActivation();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IToastActivationRegistrar) &&
            d.ImplementationType == typeof(ToastActivationRegistrar) &&
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
