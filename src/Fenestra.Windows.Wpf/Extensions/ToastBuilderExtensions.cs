using Fenestra.Windows.Services;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Windows;

/// <summary>
/// Extension methods for configuring toast notifications on <see cref="FenestraBuilder"/>.
/// </summary>
public static class FenestraBuilderExtensions
{
    /// <summary>
    /// Enables Windows toast notifications via <see cref="IToastService"/>.
    /// </summary>
    public static FenestraBuilder UseToastNotifications(this FenestraBuilder builder)
    {
        builder.Services.AddSingleton<IWindowsNotificationRegistrationManager, WindowsNotificationRegistrationManager>();
        builder.Services.AddSingleton<IToastService, ToastService>();
        return builder;
    }

    /// <summary>
    /// Enables toast background activation so clicking a notification relaunches the app when it is closed.
    /// The CLSID is derived deterministically from the application's AppId.
    /// Requires <see cref="UseToastNotifications"/> to be called first.
    /// </summary>
    public static FenestraBuilder UseToastActivation(this FenestraBuilder builder)
    {
        builder.Services.AddSingleton<IToastActivationRegistrar, ToastActivationRegistrar>();
        return builder;
    }

    /// <summary>
    /// Enables toast background activation with an explicit COM activator CLSID.
    /// Use this overload when you need a stable, hardcoded GUID (recommended for production).
    /// Generate one with <c>Guid.NewGuid()</c> and never change it once deployed.
    /// Requires <see cref="UseToastNotifications"/> to be called first.
    /// </summary>
    public static FenestraBuilder UseToastActivation(this FenestraBuilder builder, Guid activatorClsid)
    {
        builder.Services.AddSingleton(new ToastActivationOptions { ActivatorClsid = activatorClsid });
        builder.Services.AddSingleton<IToastActivationRegistrar, ToastActivationRegistrar>();
        return builder;
    }
}
