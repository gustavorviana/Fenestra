using System;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

internal static class ShellNativeMethods
{
    // General
    internal const uint WM_USER = 0x0400;

    // Shell_NotifyIcon dwMessage
    internal const uint NIM_ADD = 0x00;
    internal const uint NIM_MODIFY = 0x01;
    internal const uint NIM_DELETE = 0x02;
    internal const uint NIM_SETVERSION = 0x04;

    internal const uint NOTIFYICON_VERSION_4 = 4;

    // NIF_* flags
    internal const uint NIF_MESSAGE = 0x01;
    internal const uint NIF_ICON = 0x02;
    internal const uint NIF_TIP = 0x04;
    internal const uint NIF_STATE = 0x08;
    internal const uint NIF_INFO = 0x10;
    internal const uint NIF_GUID = 0x20;
    internal const uint NIF_REALTIME = 0x40;
    internal const uint NIF_SHOWTIP = 0x80;

    // NIIF_* balloon icon flags
    internal const uint NIIF_NONE = 0x00;
    internal const uint NIIF_INFO = 0x01;
    internal const uint NIIF_WARNING = 0x02;
    internal const uint NIIF_ERROR = 0x03;

    // System icon
    internal static readonly IntPtr IDI_APPLICATION = (IntPtr)32512;

    /// <summary>
    /// NOTIFYICONDATAW — matches the Windows SDK layout including guidItem and hBalloonIcon.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    // Shell
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, int nIcons);

    // Window / messaging
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    internal static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    // Icon
    [DllImport("user32.dll")]
    internal static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    internal static extern uint GetDoubleClickTime();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr CreateIconFromResourceEx(
        byte[] presbits, int dwResSize, bool fIcon, int dwVer, int cxDesired, int cyDesired, int flags);

}
