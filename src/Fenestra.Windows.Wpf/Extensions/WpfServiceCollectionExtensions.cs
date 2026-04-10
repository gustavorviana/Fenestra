using Fenestra.Windows;
using Fenestra.Wpf.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering WPF-specific Fenestra services in <see cref="IServiceCollection"/>.
/// These services depend on WPF infrastructure (HwndSource, Window, Dispatcher).
/// </summary>
public static class WpfServiceCollectionExtensions
{
    /// <summary>
    /// Registers the system tray icon service (<see cref="ITrayIconService"/>).
    /// Requires WPF. Windows only.
    /// </summary>
    public static IServiceCollection AddWpfTrayIcon(this IServiceCollection services)
    {
        services.AddSingleton<ITrayIconService, TrayIconService>();
        return services;
    }

    /// <summary>
    /// Registers minimize-to-tray behavior for windows implementing <see cref="IMinimizeToTray"/>.
    /// Implies <see cref="AddWpfTrayIcon"/>. Requires WPF. Windows only.
    /// </summary>
    public static IServiceCollection AddWpfMinimizeToTray(this IServiceCollection services, Action<MinimizeToTrayOptions>? configure = null)
    {
        services.AddWpfTrayIcon();
        var options = new MinimizeToTrayOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<MinimizeToTrayService>();
        return services;
    }

    /// <summary>
    /// Registers single instance mode via <see cref="SingleInstanceGuard"/>.
    /// Requires WPF. Windows only.
    /// </summary>
    public static IServiceCollection AddWpfSingleInstance(this IServiceCollection services)
    {
        services.AddSingleton<SingleInstanceGuard>();
        return services;
    }

    /// <summary>
    /// Registers global hotkey registration via <see cref="IGlobalHotkeyService"/>.
    /// Requires WPF (HwndSource for message loop). Windows only.
    /// </summary>
    public static IServiceCollection AddWpfGlobalHotkeys(this IServiceCollection services)
    {
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        return services;
    }

}
