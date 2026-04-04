using Fenestra.Core;
using Fenestra.Toast.Windows.Services;
using Fenestra.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Toast.Windows;

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
    /// Registers the application for toast background activation (relaunch when clicked after app is closed).
    /// Requires a stable GUID that must never change once deployed.
    /// </summary>
    public static FenestraBuilder UseToastActivation(this FenestraBuilder builder, Action<ToastActivationOptions> configure)
    {
        var options = new ToastActivationOptions();
        configure(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IToastActivationRegistrar, ToastActivationRegistrar>();
        return builder;
    }
}
