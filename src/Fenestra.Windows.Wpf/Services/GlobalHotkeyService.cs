using System.Windows.Interop;
using Fenestra.Windows;
using Fenestra.Windows.Models;
using Fenestra.Wpf.Native;

namespace Fenestra.Wpf.Services;

internal class GlobalHotkeyService : IGlobalHotkeyService
{
    private readonly HwndSource _hwndSource;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 1;
    private bool _disposed;

    public GlobalHotkeyService()
    {
        var parameters = new HwndSourceParameters("FenestraHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public int Register(HotkeyModifiers modifiers, HotkeyKey key, Action callback)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GlobalHotkeyService));

        var id = _nextId++;

        if (!NativeMethods.RegisterHotKey(_hwndSource.Handle, id, (uint)modifiers, (uint)key))
            throw new InvalidOperationException($"Failed to register hotkey (id={id}, modifiers={modifiers}, key={key}).");

        _callbacks[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        if (_disposed) return;

        NativeMethods.UnregisterHotKey(_hwndSource.Handle, id);
        _callbacks.Remove(id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var callback))
            {
                callback();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var id in _callbacks.Keys)
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, id);

        _callbacks.Clear();
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }
}
