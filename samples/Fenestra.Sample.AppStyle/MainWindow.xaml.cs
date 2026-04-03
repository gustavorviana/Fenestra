using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Core.Tray;
using Fenestra.Wpf.Tray;

namespace Fenestra.Sample.AppStyle;

public partial class MainWindow : Window, IMinimizeToTray
{
    private readonly ITrayIconService _tray;
    private readonly ITaskbarService _taskbar;
    private readonly AnimatedTryIcon _animatedIcon;
    private readonly NotifyBadgeOverlay _badge = new();

    public MainWindow(ITrayIconService tray, ITaskbarService taskbar)
    {
        InitializeComponent();
        _tray = tray;
        _taskbar = taskbar;

        _tray.SetTooltip("Fenestra App Style");
        _tray.MenuTheme = TrayMenuTheme.System;
        _tray.MenuCornerRadius = 6;

        _animatedIcon = new AnimatedTryIcon(
            CreateAnimationFrames().Select(ms => (ITrayIcon)new StaticTrayIcon(ms)),
            intervalMs: 300);
        _animatedIcon.Initialize();

        _tray.SetOverlay(_badge);

        var hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512);
        var appIcon = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        _tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show Window", () => { Show(); WindowState = WindowState.Normal; Activate(); })
                { Foreground = "#2196F3", Icon = appIcon },
            new TrayMenuItem("Settings (disabled)", () => { })
                { IsEnabled = false, Icon = appIcon, Foreground = "#757575" },
            TrayMenuItem.Separator(),
            new TrayMenuItem("Animation", () => { }) { Children = new[]
            {
                new TrayMenuItem("Start Animation", () => { _tray.SetIcon(_animatedIcon); _animatedIcon.StartIconAnimation(); }),
                new TrayMenuItem("Stop Animation", () => _animatedIcon.StopIconAnimation()),
            }},
            new TrayMenuItem("Badge", () => { }) { Children = new[]
            {
                new TrayMenuItem("Set Badge 3", () => _badge.Quantity = 3),
                new TrayMenuItem("Set Badge 9+", () => _badge.Quantity = 150),
                new TrayMenuItem("Set Badge Dot", () => _badge.SetDot()),
                new TrayMenuItem("Clear Badge", () => _badge.Clear()),
            }},
            new TrayMenuItem("Theme", () => { }) { Children = new[]
            {
                new TrayMenuItem("System", () => { _tray.MenuBackground = null; _tray.MenuTheme = TrayMenuTheme.System; }),
                new TrayMenuItem("Dark", () => { _tray.MenuBackground = null; _tray.MenuTheme = TrayMenuTheme.Dark; }),
                new TrayMenuItem("Light", () => { _tray.MenuBackground = null; _tray.MenuTheme = TrayMenuTheme.Light; }),
                new TrayMenuItem("Blue", () => _tray.MenuBackground = "#1E3A5F"),
                new TrayMenuItem("Default", () => { _tray.MenuBackground = null; _tray.MenuTheme = TrayMenuTheme.Default; }),
            }},
            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => Application.Current.Shutdown())
                { Foreground = "#F44336" }
        });
    }

    private void OnShowTray(object sender, RoutedEventArgs e)
    {
        _tray.Show();
        StatusText.Text = "Tray icon visible - double-click to restore";
    }

    private void OnShowBalloon(object sender, RoutedEventArgs e)
    {
        _tray.Show();
        _tray.ShowBalloonTip("App Style", "Notification from FenestraApp!", TrayBalloonIcon.Info);
    }

    private async void OnSimulateProgress(object sender, RoutedEventArgs e)
    {
        for (int i = 0; i <= 100; i += 5)
        {
            _taskbar.SetProgress(i / 100.0);
            StatusText.Text = $"Progress: {i}%";
            await Task.Delay(100);
        }

        _taskbar.SetProgressState(TaskbarProgressState.Paused);
        StatusText.Text = "Done!";
    }

    private void OnClearProgress(object sender, RoutedEventArgs e)
    {
        _taskbar.ClearProgress();
        StatusText.Text = "";
    }

    /// <summary>
    /// Generates 4 animation frames: colored circles rotating through red, green, blue, yellow.
    /// Each frame is a valid PNG stream that CreateIconFromResourceEx accepts on Vista+.
    /// </summary>
    private static List<MemoryStream> CreateAnimationFrames()
    {
        var colors = new[]
        {
            Color.FromRgb(0xF4, 0x43, 0x36), // red
            Color.FromRgb(0x4C, 0xAF, 0x50), // green
            Color.FromRgb(0x21, 0x96, 0xF3), // blue
            Color.FromRgb(0xFF, 0xC1, 0x07), // yellow
        };

        var frames = new List<MemoryStream>();
        foreach (var color in colors)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Filled circle with a white border
                var brush = new SolidColorBrush(color);
                var pen = new Pen(Brushes.White, 1.5);
                dc.DrawEllipse(brush, pen, new Point(8, 8), 7, 7);
            }

            var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(ms);
            ms.Position = 0;
            frames.Add(ms);
        }

        return frames;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
}
