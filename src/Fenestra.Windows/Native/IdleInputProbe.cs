using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Concrete <see cref="IIdleInputProbe"/> implementation over user32 / kernel32.
/// Not tested directly — all logic above this layer is tested via the mockable interface.
/// </summary>
internal sealed class IdleInputProbe : IIdleInputProbe
{
    public TimeSpan GetIdleTime()
    {
        var lii = new IdleNative.LASTINPUTINFO
        {
            cbSize = (uint)Marshal.SizeOf<IdleNative.LASTINPUTINFO>()
        };

        if (!IdleNative.GetLastInputInfo(ref lii))
            return TimeSpan.Zero; // on failure, assume active (safer than treating as idle)

        uint now = IdleNative.GetTickCount();
        // unchecked: handles tick wraparound at ~49.7 days correctly.
        // Both values are uint milliseconds since system boot.
        uint diffMs = unchecked(now - lii.dwTime);

        return TimeSpan.FromMilliseconds(diffMs);
    }
}
