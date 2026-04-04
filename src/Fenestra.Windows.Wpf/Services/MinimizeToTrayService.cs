using Fenestra.Core;
using Fenestra.Windows;
using System.ComponentModel;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class MinimizeToTrayService
{
    private readonly ITrayIconService _tray;
    private readonly bool _autoShowTrayIcon;
    private readonly bool _restoreOnDoubleClick;
    private readonly HashSet<Window> _attached = new();

    public MinimizeToTrayService(ITrayIconService tray, MinimizeToTrayOptions options)
    {
        _tray = tray;
        _autoShowTrayIcon = options.AutoShowTrayIcon;
        _restoreOnDoubleClick = options.RestoreOnDoubleClick;

        if (_restoreOnDoubleClick)
        {
            _tray.DoubleClick += OnTrayDoubleClick;
        }
    }

    public void Attach(Window window)
    {
        if (window is not IMinimizeToTray) return;
        if (!_attached.Add(window)) return;

        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (sender is not Window window) return;

        // Allow close when app is shutting down
        if (Application.Current == null ||
            Application.Current.Dispatcher.HasShutdownStarted ||
            Application.Current.Dispatcher.HasShutdownFinished)
            return;

        e.Cancel = true;
        window.Hide();

        if (_autoShowTrayIcon)
            _tray.Show();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Closing -= OnWindowClosing;
            window.Closed -= OnWindowClosed;
            _attached.Remove(window);
        }
    }

    private void OnTrayDoubleClick(object? sender, EventArgs e)
    {
        // Restore the first hidden window that has IMinimizeToTray
        foreach (var window in _attached)
        {
            if (!window.IsVisible)
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
                return;
            }
        }
    }
}

public class MinimizeToTrayOptions
{
    /// <summary>
    /// Automatically show the tray icon when a window is hidden. Default: true.
    /// </summary>
    public bool AutoShowTrayIcon { get; set; } = true;

    /// <summary>
    /// Restore the window on tray icon double-click. Default: true.
    /// </summary>
    public bool RestoreOnDoubleClick { get; set; } = true;
}
