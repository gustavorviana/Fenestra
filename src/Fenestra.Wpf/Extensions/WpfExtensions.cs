using Fenestra.Core.Drawing;
using System.Windows.Media;

namespace Fenestra.Wpf.Extensions;

internal static class WpfExtensions
{
    public static Brush ToBrush(this FenestralColor color)
        => new SolidColorBrush(color.ToMediaColor());

    public static Color ToMediaColor(this FenestralColor color)
        => Color.FromRgb(color.R, color.G, color.B);
}