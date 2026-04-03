using Fenestra.Core.Native;
using Fenestra.Core.Tray;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Native;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fenestra.Wpf.Tray;

/// <summary>
/// WPF implementation of badge overlay rendering for the tray icon.
/// </summary>
public class NotifyBadgeOverlay : NotifyBadgeOverlayBase
{
    private SafeIconHandle? _badgedIconHandle;

    protected override IntPtr RenderBadgedIcon(IntPtr baseHIcon)
    {
        _badgedIconHandle?.Dispose();

        const int size = 16;

        var bgBrush = Background.ToBrush();
        var fgBrush = Foreground.ToBrush();

        var baseBitmap = Imaging.CreateBitmapSourceFromHIcon(
            baseHIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(baseBitmap, new Rect(0, 0, size, size));

            if (IsDot)
            {
                dc.DrawEllipse(bgBrush, null, new Point(12, 12), 4, 4);
            }
            else if (Quantity > 0)
            {
                string text = Quantity > 9 ? "9" : Quantity.ToString();
                int badgeSize = text.Length > 1 ? 10 : 8;
                double badgeX = size - badgeSize;
                double badgeY = size - badgeSize;

                dc.DrawEllipse(bgBrush, null,
                    new Point(badgeX + badgeSize / 2.0, badgeY + badgeSize / 2.0),
                    badgeSize / 2.0, badgeSize / 2.0);

                var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                var formatted = new FormattedText(
                    text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeface, text.Length > 1 ? 6 : 7, fgBrush,
                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                double textX = badgeX + (badgeSize - formatted.Width) / 2.0;
                double textY = badgeY + (badgeSize - formatted.Height) / 2.0;
                dc.DrawText(formatted, new Point(textX, textY));
            }
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var pixels = new byte[size * size * 4];
        rtb.CopyPixels(pixels, size * 4, 0);

        var hbmColor = NativeMethods.CreateBitmap(size, size, 1, 32, IntPtr.Zero);
        if (hbmColor == IntPtr.Zero) return IntPtr.Zero;

        var pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            NativeMethods.DeleteObject(hbmColor);
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
            hbmColor = hbmColor
        };

        var hIcon = NativeMethods.CreateIconIndirect(ref iconInfo);

        NativeMethods.DeleteObject(hbmColor);
        NativeMethods.DeleteObject(hbmMask);

        if (hIcon != IntPtr.Zero)
            _badgedIconHandle = new SafeIconHandle(hIcon, ownsHandle: true);

        return hIcon;
    }
}