using Fenestra.Core.Drawing;
using Fenestra.Core.Models;

namespace Fenestra.Core.Tray;

public interface ITrayMenuStyle
{
    FenestralColor? Background { get; set; }
    FenestralColor? Foreground { get; set; }
    double CornerRadius { get; set; }
    TrayMenuTheme Theme { get; set; }
}