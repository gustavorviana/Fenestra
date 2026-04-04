namespace Fenestra.Windows.Models;

/// <summary>
/// Specifies the application theme mode.
/// </summary>
public enum AppThemeMode
{
    /// <summary>
    /// Follow the Windows system theme (monitors registry for changes).
    /// </summary>
    System,

    /// <summary>
    /// Force dark mode. Registry monitoring is disabled.
    /// </summary>
    Dark,

    /// <summary>
    /// Force light mode. Registry monitoring is disabled.
    /// </summary>
    Light
}
