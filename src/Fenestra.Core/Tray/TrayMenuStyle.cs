using Fenestra.Core.Drawing;
using Fenestra.Core.Models;

namespace Fenestra.Core.Tray;

public abstract class TrayMenuStyle
{
    public FenestralColor? Background { get; set; }
    public FenestralColor? Foreground { get; set; }
    public double CornerRadius { get; set; }
    public TrayMenuTheme Theme { get; set; }

    public void ApplyTheme(object menu, bool isSystemDarkMode)
    {
        var colors = Resolve(isSystemDarkMode);
        var hasCustomTheme = colors.Background != null || CornerRadius > 0;

        if (hasCustomTheme)
            OnApplyTheme(menu, colors, CornerRadius);
    }

    protected virtual void OnApplyTheme(object menu, TrayMenuColors colors, double cornerRadius)
    {
    }

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