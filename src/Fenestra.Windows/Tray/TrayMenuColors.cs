using Fenestra.Core.Drawing;

namespace Fenestra.Windows.Tray;

/// <summary>
/// Resolved color values for the tray context menu theme.
/// </summary>
public readonly struct TrayMenuColors
{
    /// <summary>
    /// Gets the menu background color.
    /// </summary>
    public FenestralColor? Background { get; }

    /// <summary>
    /// Gets the menu text color.
    /// </summary>
    public FenestralColor? Foreground { get; }

    /// <summary>
    /// Gets the menu border color.
    /// </summary>
    public FenestralColor? Border { get; }

    /// <summary>
    /// Gets the separator line color.
    /// </summary>
    public FenestralColor? Separator { get; }

    /// <summary>
    /// Initializes a new instance with the specified color values.
    /// </summary>
    public TrayMenuColors(FenestralColor? background, FenestralColor? foreground, FenestralColor? border, FenestralColor? separator)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        Separator = separator;
    }
}