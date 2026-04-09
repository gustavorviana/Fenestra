using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// P/Invoke declarations for user32 idle detection APIs.
/// Wrapped by <see cref="IdleInputProbe"/> into a managed surface.
/// </summary>
internal static class IdleNative
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime; // tick count at last input (ms since system boot)
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    internal static extern uint GetTickCount();
}
