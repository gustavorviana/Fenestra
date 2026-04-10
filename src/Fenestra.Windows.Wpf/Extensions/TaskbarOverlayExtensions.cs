using Fenestra.Windows;
using Fenestra.Wpf.Native;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fenestra.Wpf;

/// <summary>
/// WPF bridge for <see cref="ITaskbarOverlayService"/> — adds overloads that accept
/// <see cref="ImageSource"/> and convenience badge builders, rendering to 16×16 HICONs
/// and handing ownership to the framework-agnostic service.
/// </summary>
public static class TaskbarOverlayExtensions
{
    /// <summary>
    /// Renders <paramref name="icon"/> into a 16×16 HICON and sets it as the taskbar
    /// overlay.
    /// </summary>
    /// <param name="service">The overlay service.</param>
    /// <param name="icon">Any WPF <see cref="ImageSource"/>. Ideally already 16×16.</param>
    /// <param name="accessibilityText">Optional screen-reader description.</param>
    public static void SetOverlay(
        this ITaskbarOverlayService service,
        ImageSource icon,
        string? accessibilityText = null)
    {
        if (service is null) throw new ArgumentNullException(nameof(service));
        if (icon is null) throw new ArgumentNullException(nameof(icon));

        var hIcon = CreateHiconFromImageSource(icon);
        if (hIcon == IntPtr.Zero) return;

        service.SetOverlay(hIcon, accessibilityText);
    }

    /// <summary>
    /// Sets a circular badge overlay with a single character (e.g. "3", "!", "✓")
    /// centered on a colored background. No binary assets needed — the badge is
    /// rendered entirely in code at 16×16.
    /// </summary>
    /// <param name="service">The overlay service.</param>
    /// <param name="glyph">Single character to display (e.g. "3", "!", "✓").</param>
    /// <param name="background">Fill color of the circle.</param>
    /// <param name="foreground">Color of the glyph text.</param>
    /// <param name="accessibilityText">Optional screen-reader description.</param>
    public static void SetBadgeOverlay(
        this ITaskbarOverlayService service,
        string glyph,
        Color background,
        Color foreground,
        string? accessibilityText = null)
    {
        if (service is null) throw new ArgumentNullException(nameof(service));
        if (string.IsNullOrEmpty(glyph)) throw new ArgumentException("Glyph must not be empty.", nameof(glyph));

        var badge = BuildBadgeIcon(glyph, background, foreground);
        service.SetOverlay(badge, accessibilityText);
    }

    /// <summary>
    /// Builds a 16×16 circular badge <see cref="ImageSource"/> with a single character
    /// centered on a colored background.
    /// </summary>
    private static ImageSource BuildBadgeIcon(string glyph, Color background, Color foreground)
    {
        const double size = 16.0;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawEllipse(
                new SolidColorBrush(background),
                null,
                new Point(size / 2, size / 2),
                size / 2,
                size / 2);

            var typeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Bold,
                FontStretches.Normal);

            var formatted = new FormattedText(
                glyph,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                new SolidColorBrush(foreground),
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);

            dc.DrawText(
                formatted,
                new Point(
                    (size - formatted.Width) / 2,
                    (size - formatted.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(
            (int)size, (int)size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static IntPtr CreateHiconFromImageSource(ImageSource source)
    {
        const int size = 16;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(source, new Rect(0, 0, size, size));
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var pixels = new byte[size * size * 4];
        rtb.CopyPixels(pixels, size * 4, 0);

        IntPtr hbmColor;
        var pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            hbmColor = NativeMethods.CreateBitmap(size, size, 1, 32, pinnedPixels.AddrOfPinnedObject());
        }
        finally
        {
            pinnedPixels.Free();
        }
        if (hbmColor == IntPtr.Zero) return IntPtr.Zero;

        var hbmMask = NativeMethods.CreateBitmap(size, size, 1, 1, IntPtr.Zero);
        if (hbmMask == IntPtr.Zero)
        {
            NativeMethods.DeleteObject(hbmColor);
            return IntPtr.Zero;
        }

        var iconInfo = new NativeMethods.ICONINFO
        {
            fIcon = true,
            xHotspot = 0,
            yHotspot = 0,
            hbmMask = hbmMask,
            hbmColor = hbmColor,
        };

        var hIcon = NativeMethods.CreateIconIndirect(ref iconInfo);

        NativeMethods.DeleteObject(hbmColor);
        NativeMethods.DeleteObject(hbmMask);

        return hIcon;
    }
}
