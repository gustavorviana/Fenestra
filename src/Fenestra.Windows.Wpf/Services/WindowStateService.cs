using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Wpf.Native;
using System.Windows;

namespace Fenestra.Wpf.Services;

internal class WindowStateService
{
    private readonly IWindowPositionStorage _storage;

    public WindowStateService(IWindowPositionStorage storage)
    {
        _storage = storage;
    }

    public void Attach(Window window)
    {
        if (window is not IRememberWindowState) return;

        var key = GetKey(window);
        var data = _storage.Load(key);

        if (data != null)
        {
            if (IsVisibleOnAnyMonitor(data))
            {
                window.Left = data.Left;
                window.Top = data.Top;
                window.Width = data.Width;
                window.Height = data.Height;
                window.WindowState = (WindowState)data.State;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
            }
            else
            {
                _storage.Delete(key);
            }
        }

        window.Closing += (_, _) => Save(key, window);
        window.IsVisibleChanged += (_, _) =>
        {
            if (!window.IsVisible)
                Save(key, window);
        };
    }

    private void Save(string key, Window window)
    {
        var state = window.WindowState == WindowState.Minimized
            ? WindowState.Normal
            : window.WindowState;

        var data = new WindowPositionData
        {
            Left = window.RestoreBounds.Left,
            Top = window.RestoreBounds.Top,
            Width = window.RestoreBounds.Width,
            Height = window.RestoreBounds.Height,
            State = (int)state
        };

        _storage.Save(key, data);
    }

    private static string GetKey(Window window)
    {
        return window.GetType().FullName ?? window.GetType().Name;
    }

    private static bool IsVisibleOnAnyMonitor(WindowPositionData data)
    {
        var rect = new NativeMethods.RECT
        {
            Left = (int)data.Left,
            Top = (int)data.Top,
            Right = (int)(data.Left + data.Width),
            Bottom = (int)(data.Top + data.Height)
        };

        return NativeMethods.MonitorFromRect(ref rect, NativeMethods.MONITOR_DEFAULTTONULL) != IntPtr.Zero;
    }
}
