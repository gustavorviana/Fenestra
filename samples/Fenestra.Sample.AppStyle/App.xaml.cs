using Fenestra.Core;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Windows.Tray;
using Fenestra.Wpf;
using Fenestra.Wpf.Tray;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fenestra.Sample.AppStyle;

public partial class App : FenestraApp
{
    private readonly NotifyBadgeOverlay _badge = new();
    private AnimatedTryIcon _animatedIcon = null!;

    protected override void Configure(FenestraBuilder builder)
    {
        builder.Services.AddWpfMinimizeToTray();
        builder.Services.AddWpfSingleInstance();
        builder.Services.AddWpfGlobalHotkeys();
        builder.Services.AddWindowsAutoStart();
        builder.Services.AddWindowsToastNotifications();
        builder.Services.AddWindowsToastActivation();
        builder.RegisterWindows();
    }

    protected override Window CreateMainWindow(IServiceProvider services)
    {
        return services.GetRequiredService<MainWindow>();
    }

    protected override void OnReady(IServiceProvider services, Window mainWindow)
    {
        var tray = services.GetRequiredService<ITrayIconService>();
        var bus = services.GetRequiredService<IEventBus>();
        var hotkeys = services.GetRequiredService<IGlobalHotkeyService>();
        var autoStart = services.GetRequiredService<IAutoStartService>();

        SetupTray(tray, bus);
        SetupEventBus(tray, bus, autoStart, mainWindow);
        SetupHotkeys(hotkeys, bus);
    }

    private void SetupTray(ITrayIconService tray, IEventBus bus)
    {
        tray.Show();
        tray.SetTooltip("Fenestra Sample — All Features");
        tray.MenuStyle!.Theme = TrayMenuTheme.System;
        tray.MenuStyle!.CornerRadius = 6;

        _animatedIcon = new AnimatedTryIcon(
            CreateAnimationFrames().Select(ms => (ITrayIcon)new StaticTrayIcon(ms)),
            intervalMs: 300);
        _animatedIcon.Initialize();

        tray.SetOverlay(_badge);

        tray.Click += (_, _) => bus.PublishAsync(new ShowWindowEvent());
        tray.DoubleClick += (_, _) => bus.PublishAsync(new ShowWindowEvent());

        var hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512);
        var appIcon = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        tray.SetContextMenu(new[]
        {
            new TrayMenuItem("Show Window", () => bus.PublishAsync(new ShowWindowEvent()))
                { Foreground = "#2196F3", Icon = appIcon },
            TrayMenuItem.Separator(),

            new TrayMenuItem("Animation", () => { }) { Children = new[]
            {
                new TrayMenuItem("Start", () => bus.PublishAsync(new AnimationToggleEvent(true))),
                new TrayMenuItem("Stop", () => bus.PublishAsync(new AnimationToggleEvent(false))),
            }},

            new TrayMenuItem("Badge", () => { }) { Children = new[]
            {
                new TrayMenuItem("Set 3", () => bus.PublishAsync(new BadgeChangedEvent(Quantity: 3))),
                new TrayMenuItem("Set 9+", () => bus.PublishAsync(new BadgeChangedEvent(Quantity: 150))),
                new TrayMenuItem("Dot", () => bus.PublishAsync(new BadgeChangedEvent(IsDot: true))),
                new TrayMenuItem("Clear", () => bus.PublishAsync(new BadgeChangedEvent(IsClear: true))),
            }},

            new TrayMenuItem("Theme", () => { }) { Children = new[]
            {
                new TrayMenuItem("System", () => bus.PublishAsync(new ThemeChangedEvent(TrayMenuTheme.System))),
                new TrayMenuItem("Dark", () => bus.PublishAsync(new ThemeChangedEvent(TrayMenuTheme.Dark))),
                new TrayMenuItem("Light", () => bus.PublishAsync(new ThemeChangedEvent(TrayMenuTheme.Light))),
                new TrayMenuItem("Blue", () => bus.PublishAsync(new ThemeChangedEvent(null, "#1E3A5F"))),
                new TrayMenuItem("Default", () => bus.PublishAsync(new ThemeChangedEvent(TrayMenuTheme.Default))),
            }},

            new TrayMenuItem("Balloon", () => { }) { Children = new[]
            {
                new TrayMenuItem("Info", () => bus.PublishAsync(new BalloonRequestEvent("Info", "This is an info balloon.", TrayBalloonIcon.Info))),
                new TrayMenuItem("Warning", () => bus.PublishAsync(new BalloonRequestEvent("Warning", "This is a warning.", TrayBalloonIcon.Warning))),
                new TrayMenuItem("Error", () => bus.PublishAsync(new BalloonRequestEvent("Error", "Something went wrong!", TrayBalloonIcon.Error))),
            }},

            new TrayMenuItem("Auto-Start", () => { }) { Children = new[]
            {
                new TrayMenuItem("Enable", () => bus.PublishAsync(new AutoStartToggleEvent(true))),
                new TrayMenuItem("Disable", () => bus.PublishAsync(new AutoStartToggleEvent(false))),
            }},

            TrayMenuItem.Separator(),
            new TrayMenuItem("Exit", () => bus.PublishAsync(new ExitRequestEvent()))
                { Foreground = "#F44336" }
        });
    }

    private void SetupEventBus(ITrayIconService tray, IEventBus bus, IAutoStartService autoStart, Window mainWindow)
    {
        bus.On<ShowWindowEvent>(_ =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
            return Task.CompletedTask;
        });

        bus.On<ThemeChangedEvent>(e =>
        {
            if (e.CustomBackground != null)
                tray.MenuStyle!.Background = e.CustomBackground;
            else
            {
                tray.MenuStyle!.Background = null;
                tray.MenuStyle!.Theme = e.Theme ?? TrayMenuTheme.Default;
            }
            return Task.CompletedTask;
        });

        bus.On<BadgeChangedEvent>(e =>
        {
            if (e.IsClear) _badge.Clear();
            else if (e.IsDot) _badge.SetDot();
            else if (e.Quantity.HasValue) _badge.Quantity = e.Quantity.Value;
            return Task.CompletedTask;
        });

        bus.On<AnimationToggleEvent>(e =>
        {
            if (e.Start)
            {
                tray.SetIcon(_animatedIcon);
                _animatedIcon.StartIconAnimation();
            }
            else
            {
                _animatedIcon.StopIconAnimation();
            }
            return Task.CompletedTask;
        });

        bus.On<BalloonRequestEvent>(e =>
        {
            tray.Show();
            tray.ShowBalloonTip(e.Title, e.Text, e.Icon);
            return Task.CompletedTask;
        });

        bus.On<AutoStartToggleEvent>(e =>
        {
            if (e.Enable) autoStart.Enable();
            else autoStart.Disable();
            return Task.CompletedTask;
        });

        bus.On<ExitRequestEvent>(_ =>
        {
            Current.Shutdown();
            return Task.CompletedTask;
        });
    }

    private static void SetupHotkeys(IGlobalHotkeyService hotkeys, IEventBus bus)
    {
        hotkeys.Register(HotkeyModifiers.Ctrl | HotkeyModifiers.Alt, HotkeyKey.F,
            () => bus.PublishAsync(new ShowWindowEvent()));
    }

    private static List<MemoryStream> CreateAnimationFrames()
    {
        var colors = new[]
        {
            Color.FromRgb(0xF4, 0x43, 0x36),
            Color.FromRgb(0x4C, 0xAF, 0x50),
            Color.FromRgb(0x21, 0x96, 0xF3),
            Color.FromRgb(0xFF, 0xC1, 0x07),
        };

        var frames = new List<MemoryStream>();
        foreach (var color in colors)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawEllipse(new SolidColorBrush(color), new Pen(Brushes.White, 1.5), new Point(8, 8), 7, 7);
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
