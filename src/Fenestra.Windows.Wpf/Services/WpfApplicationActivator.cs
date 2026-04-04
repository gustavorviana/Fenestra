using Fenestra.Core;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class WpfApplicationActivator : IApplicationActivator
{
    public void BringToForeground()
    {
        var window = Application.Current?.MainWindow;
        if (window == null) return;

        if (!window.IsVisible)
            window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
        window.Focus();
    }
}
