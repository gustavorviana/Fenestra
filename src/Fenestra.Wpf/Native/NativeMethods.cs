using System;
using System.Runtime.InteropServices;

namespace Fenestra.Wpf.Native;

/// <summary>
/// P/Invoke declarations used only by WPF-specific code (context menu, badge rendering).
/// Shell/icon/notification interop lives in <c>Fenestra.Core.Native.NativeMethods</c>.
/// </summary>
internal static class NativeMethods
{
    // TrackPopupMenu flags
    internal const int TPM_RIGHTBUTTON = 0x0002;
    internal const int TPM_RETURNCMD = 0x0100;

    // Menu flags
    internal const int MF_STRING = 0x0000;
    internal const int MF_SEPARATOR = 0x0800;
    internal const int MF_GRAYED = 0x0001;
    internal const int MF_POPUP = 0x0010;

    // Badge icon creation
    [StructLayout(LayoutKind.Sequential)]
    internal struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    // Menu
    [DllImport("user32.dll")]
    internal static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    internal static extern bool DestroyMenu(IntPtr hMenu);

    // Window
    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    // Icon
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr CreateIconIndirect(ref ICONINFO piconInfo);

    [DllImport("user32.dll")]
    internal static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconInfo);

    // GDI
    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);

    // Hotkeys
    internal const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Monitor
    internal const uint MONITOR_DEFAULTTONULL = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);
}
