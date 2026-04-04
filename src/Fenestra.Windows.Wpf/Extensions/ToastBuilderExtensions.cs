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
}
