using Fenestra.Core.Drawing;
using Fenestra.Core.Models;

namespace Fenestra.Core.Tray;

/// <summary>
/// Abstract base for tray context menu styling. Resolves theme colors and delegates platform-specific rendering to subclasses.
/// </summary>
public abstract class TrayMenuStyle
{
    /// <summary>
    /// Gets or sets the custom background color, or null to use theme defaults.
    /// </summary>
    public FenestralColor? Background { get; set; }

    /// <summary>
    /// Gets or sets the custom foreground (text) color, or null to use theme defaults.
    /// </summary>
    public FenestralColor? Foreground { get; set; }

    /// <summary>
    /// Gets or sets the corner radius for the context menu.
    /// </summary>
    public double CornerRadius { get; set; }

    /// <summary>
    /// Gets or sets the theme mode (dark, light, or system).
    /// </summary>
    public TrayMenuTheme Theme { get; set; }

    /// <summary>
    /// Resolves the theme colors and applies them to the menu if a custom theme is configured.
    /// </summary>
    public void ApplyTheme(object menu, bool isSystemDarkMode)
    {
        var colors = Resolve(isSystemDarkMode);
        var hasCustomTheme = colors.Background != null || CornerRadius > 0;

        if (hasCustomTheme)
            OnApplyTheme(menu, colors, CornerRadius);
    }

    /// <summary>
    /// Applies the resolved theme colors and corner radius to a platform-specific menu.
    /// </summary>
    protected virtual void OnApplyTheme(object menu, TrayMenuColors colors, double cornerRadius)
    {
    }

    /// <summary>
    /// Resolves the final menu colors based on the current theme and system dark mode state.
    /// </summary>
    public TrayMenuColors Resolve(bool isSystemDarkMode)
    {
        if (Background != null)
        {
            return new TrayMenuColors(
                Background,
                Foreground,
                FenestralColor.FromRgb(0x66, 0x66, 0x66),
                FenestralColor.FromRgb(0x88, 0x88, 0x88));
        }

        bool isDark;
        switch (Theme)
        {
            case TrayMenuTheme.Dark:
                isDark = true;
                break;
            case TrayMenuTheme.Light:
                isDark = false;
                break;
            case TrayMenuTheme.System:
                isDark = isSystemDarkMode;
                break;
            default:
                return default;
        }

        if (isDark)
        {
            return new TrayMenuColors(
                FenestralColor.FromRgb(0x2B, 0x2B, 0x2B),
                Foreground ?? FenestralColor.FromRgb(0xE0, 0xE0, 0xE0),
                FenestralColor.FromRgb(0x50, 0x50, 0x50),
                FenestralColor.FromRgb(0x50, 0x50, 0x50));
        }
        else
        {
            return new TrayMenuColors(
                FenestralColor.FromRgb(0xF3, 0xF3, 0xF3),
                Foreground ?? FenestralColor.FromRgb(0x1A, 0x1A, 0x1A),
                FenestralColor.FromRgb(0xCC, 0xCC, 0xCC),
                FenestralColor.FromRgb(0xD0, 0xD0, 0xD0));
        }
    }
}