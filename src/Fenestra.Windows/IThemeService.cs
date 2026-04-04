using Fenestra.Core;
using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// Detects the current app theme (dark/light) and notifies on changes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme mode (System, Dark, or Light).
    /// </summary>
    AppThemeMode Mode { get; }

    /// <summary>
    /// Gets whether the app is currently using dark mode.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Sets the theme mode. System follows Windows settings; Dark/Light are fixed and disable registry monitoring.
    /// </summary>
    void SetMode(AppThemeMode mode);

    /// <summary>
    /// Raised when the effective theme changes (dark/light).
    /// </summary>
    event BusHandler<bool>? ThemeChanged;
}
