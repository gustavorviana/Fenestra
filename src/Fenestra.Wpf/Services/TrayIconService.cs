using Fenestra.Core.Models;
using Fenestra.Core.Tray;
using Fenestra.Wpf.Extensions;
using Fenestra.Wpf.Native;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fenestra.Wpf.Services;

internal sealed class TrayIconService : TrayIconServiceBase
{
    private HwndSource? _hwndSource;

    // ---------------------------------------------------------------------------
    // Window handle — base class calls this once on first use
    // ---------------------------------------------------------------------------

    protected override IntPtr CreateWindowHandle()
    {
        var parameters = new HwndSourceParameters("FenestraTrayIcon")
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
        if (ProcessMessage((uint)msg, wParam, lParam))
        {
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    // ---------------------------------------------------------------------------
    // Context Menu (WPF ContextMenu)
    // ---------------------------------------------------------------------------

    protected override void OnShowContextMenu()
    {
        if (MenuItems == null || MenuItems.Count == 0 || _hwndSource == null) return;

        var menu = new ContextMenu();

        ResolveTheme(out var bg, out var fg, out var border, out var separator);

        bool hasCustomTheme = bg != null || MenuCornerRadius > 0;
        if (hasCustomTheme)
            ApplyMenuTheme(menu, bg ?? SystemColors.MenuBrush, fg, border, separator, MenuCornerRadius);

        BuildMenu(menu.Items, MenuItems, fg, hasCustomTheme ? bg : null);

        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

        NativeMethods.SetForegroundWindow(_hwndSource.Handle);
        menu.IsOpen = true;
    }

    // ---------------------------------------------------------------------------
    // Dispose — clean up HwndSource
    // ---------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------
    // Theme resolution
    // ---------------------------------------------------------------------------

    private void ResolveTheme(out Brush? background, out Brush? foreground, out Brush? border, out Brush? separator)
    {
        if (MenuBackground != null)
        {
            background = WpfExtensions.ParseBrush(MenuBackground);
            foreground = null;
            border = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            separator = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            return;
        }

        bool isDark;
        switch (MenuTheme)
        {
            case TrayMenuTheme.Dark:
                isDark = true;
                break;
            case TrayMenuTheme.Light:
                isDark = false;
                break;
            case TrayMenuTheme.System:
                isDark = IsWindowsDarkMode();
                break;
            default:
                background = null;
                foreground = null;
                border = null;
                separator = null;
                return;
        }

        if (isDark)
        {
            background = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B));
            foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            border = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50));
            separator = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50));
        }
        else
        {
            background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
            foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            border = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            separator = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
        }
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i) return i == 0;
        }
        catch
        {
            // Registry read failed — fall back to light.
        }

        return false;
    }

    // ---------------------------------------------------------------------------
    // WPF menu helpers
    // ---------------------------------------------------------------------------

    private static void ApplyMenuTheme(ContextMenu menu, Brush background, Brush? foreground,
        Brush? borderBrush, Brush? separatorBrush, double cornerRadius)
    {
        menu.Background = background;
        menu.HasDropShadow = true;

        var template = new ControlTemplate(typeof(ContextMenu));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, background);
        borderFactory.SetValue(Border.BorderBrushProperty, borderBrush ?? new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(4));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));

        var presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
        borderFactory.AppendChild(presenter);
        template.VisualTree = borderFactory;
        menu.Template = template;

        menu.Resources[SystemColors.MenuBrushKey] = background;
        if (foreground != null)
            menu.Resources[SystemColors.MenuTextBrushKey] = foreground;

        var menuItemStyle = new Style(typeof(MenuItem));
        menuItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        menuItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        if (foreground != null)
            menuItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, foreground));

        var separatorStyle = new Style(typeof(Separator));
        separatorStyle.Setters.Add(new Setter(Separator.BackgroundProperty, separatorBrush ?? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))));
        separatorStyle.Setters.Add(new Setter(Separator.MarginProperty, new Thickness(4, 2, 4, 2)));

        menu.Resources[typeof(MenuItem)] = menuItemStyle;
        menu.Resources[typeof(Separator)] = separatorStyle;
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
                mi.Foreground = WpfExtensions.ParseBrush(item.Foreground);
            else if (themeForeground != null)
                mi.Foreground = themeForeground;

            if (item.Background != null)
                mi.Background = WpfExtensions.ParseBrush(item.Background);

            if (item.Children is { Count: > 0 })
            {
                if (themeBackground != null)
                {
                    var bg = themeBackground;
                    var fg = themeForeground;
                    mi.SubmenuOpened += (sender, _) =>
                    {
                        if (sender is MenuItem parent)
                            StyleSubmenuPopup(parent, bg, fg);
                    };
                }

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

    private static void StyleSubmenuPopup(MenuItem parent, Brush background, Brush? foreground)
    {
        parent.UpdateLayout();

        var popup = parent.Template?.FindName("PART_Popup", parent) as System.Windows.Controls.Primitives.Popup;
        if (popup?.Child is FrameworkElement popupRoot)
        {
            ApplyBackgroundRecursive(popupRoot, background, foreground);
        }
    }

    private static void ApplyBackgroundRecursive(DependencyObject element, Brush background, Brush? foreground)
    {
        if (element is Border border)
        {
            border.Background = background;
        }
        else if (element is Panel panel)
        {
            panel.Background = background;
        }

        int count = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < count; i++)
        {
            ApplyBackgroundRecursive(VisualTreeHelper.GetChild(element, i), background, foreground);
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
