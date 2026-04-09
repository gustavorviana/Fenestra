using Fenestra.Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fenestra.Windows;

/// <summary>
/// Extension methods for registering Fenestra.Windows services in <see cref="IServiceCollection"/>.
/// These services are Windows-specific and will throw <see cref="PlatformNotSupportedException"/>
/// at runtime on non-Windows platforms.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Windows toast notification service (<see cref="IToastService"/>).
    /// Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsToastNotifications(this IServiceCollection services)
    {
        services.AddSingleton<IWindowsNotificationRegistrationManager, WindowsNotificationRegistrationManager>();
        services.AddSingleton<IToastService, ToastService>();
        return services;
    }

    /// <summary>
    /// Registers the toast background activation service (<see cref="IToastActivationRegistrar"/>).
    /// Requires <see cref="AddWindowsToastNotifications"/> to be called first.
    /// Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsToastActivation(this IServiceCollection services)
    {
        services.AddSingleton<IToastActivationRegistrar, ToastActivationRegistrar>();
        return services;
    }

    /// <summary>
    /// Registers the Windows auto-start service (<see cref="IAutoStartService"/>).
    /// Manages application startup registration via the Windows Registry.
    /// </summary>
    public static IServiceCollection AddWindowsAutoStart(this IServiceCollection services)
    {
        services.AddSingleton<IAutoStartService, AutoStartService>();
        return services;
    }

    /// <summary>
    /// Registers the Windows theme detection service (<see cref="IThemeService"/>).
    /// Monitors dark/light mode changes. Requires Windows 10 or later.
    /// </summary>
    public static IServiceCollection AddWindowsThemeDetection(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }

    /// <summary>
    /// Registers the Windows Credential Vault service (<see cref="ICredentialVault"/>).
    /// Provides DPAPI-encrypted per-user storage for secrets via the Credential Manager.
    /// Requires Windows. See <c>docs/credential-vault.md</c> for the security model.
    /// </summary>
    public static IServiceCollection AddWindowsCredentialVault(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialVault, CredentialVault>();
        return services;
    }
}
