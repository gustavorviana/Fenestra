using Fenestra.Core.Drawing;

namespace Fenestra.Core.Tray;

public readonly struct TrayMenuColors
{
    public FenestralColor? Background { get; }
    public FenestralColor? Foreground { get; }
    public FenestralColor? Border { get; }
    public FenestralColor? Separator { get; }

    public TrayMenuColors(FenestralColor? background, FenestralColor? foreground, FenestralColor? border, FenestralColor? separator)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        Separator = separator;
    }
}