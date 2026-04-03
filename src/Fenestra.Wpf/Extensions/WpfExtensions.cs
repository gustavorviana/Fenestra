using System.Windows.Media;

namespace Fenestra.Wpf.Extensions;

internal static class WpfExtensions
{
    public static Brush ParseBrush(string color)
    {
        try
        {
            var converted = ColorConverter.ConvertFromString(color);
            if (converted is Color c)
                return new SolidColorBrush(c);
        }
        catch
        {
            // ignore invalid color strings
        }

        return Brushes.Transparent;
    }
}