namespace Fenestra.Core.Models;

public enum TrayMenuTheme
{
    /// <summary>
    /// Default WPF system appearance (no custom template).
    /// </summary>
    Default,

    /// <summary>
    /// Follow the current Windows app theme (dark or light).
    /// </summary>
    System,

    /// <summary>
    /// Force dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Force light theme.
    /// </summary>
    Light
}
