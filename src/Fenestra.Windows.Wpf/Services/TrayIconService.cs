using Fenestra.Core.Models;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Windows.Tray;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Native;
using Fenestra.Wpf.Tray;
using System.IO;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fenestra.Wpf.Services;

internal sealed class TrayIconService : TrayIconServiceBase
{
    private HwndSource? _hwndSource;
    private readonly string _appId;
    private readonly IThemeService? _themeService;

    public TrayIconService(AppInfo appInfo, IServiceProvider services)
    {
        MenuStyle = CreateDefaultMenuStyle();
        _appId = appInfo.AppId;
        _themeService = services.GetService(typeof(IThemeService)) as IThemeService;
    }

    protected override IntPtr CreateWindowHandle()
    {
        var parameters = new HwndSourceParameters(_appId)
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        return _hwndSource.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        handled = ProcessMessage((uint)msg, wParam, lParam);
        return IntPtr.Zero;
    }

    protected override TrayMenuStyle CreateDefaultMenuStyle() => new WpfTrayMenuStyle();

    protected override void OnShowContextMenu()
    {
        if (MenuItems == null || MenuItems.Count == 0 || _hwndSource == null) return;

        var menu = new ContextMenu();
        Brush? fg = null;
        Brush? bg = null;

        if (MenuStyle != null)
        {
            MenuStyle.ApplyTheme(menu, GetIsDarkMode());
            var colors = MenuStyle.Resolve(GetIsDarkMode());
            fg = colors.Foreground?.ToBrush();
            bg = colors.Background?.ToBrush();
        }

        BuildMenu(menu.Items, MenuItems, fg, bg);

        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

        NativeMethods.SetForegroundWindow(_hwndSource.Handle);
        menu.IsOpen = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        base.Dispose(disposing);
    }

    private bool GetIsDarkMode()
    {
        if (_themeService != null)
            return _themeService.IsDarkMode;

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i) return i == 0;
        }
        catch { }

        return false;
    }

    private static void BuildMenu(ItemCollection target, IReadOnlyList<TrayMenuItem> items,
        Brush? themeForeground = null, Brush? themeBackground = null)
    {
        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                target.Add(new Separator());
                continue;
            }

            var mi = new MenuItem
            {
                Header = item.Text,
                IsEnabled = item.IsEnabled
            };

            if (item.Icon != null)
                mi.Icon = CreateIcon(item.Icon);

            if (item.Foreground != null)
                mi.Foreground = item.Foreground?.ToBrush();
            else if (themeForeground != null)
                mi.Foreground = themeForeground;

            if (item.Background != null)
                mi.Background = item.Background?.ToBrush();

            if (item.Children is { Count: > 0 })
            {
                BuildMenu(mi.Items, item.Children, themeForeground, themeBackground);
            }
            else if (item.Action != null)
            {
                var action = item.Action;
                mi.Click += (_, _) => action();
            }

            target.Add(mi);
        }
    }

    private static object CreateIcon(object iconSource)
    {
        ImageSource? imageSource = null;

        if (iconSource is string path)
        {
            imageSource = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }
        else if (iconSource is Stream stream)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = stream;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            imageSource = bmp;
        }
        else if (iconSource is ImageSource src)
        {
            imageSource = src;
        }

        if (imageSource == null) return iconSource;

        return new Image
        {
            Source = imageSource,
            Width = 16,
            Height = 16
        };
    }
}