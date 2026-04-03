using Fenestra.Core.Native;
using System.Runtime.InteropServices;

namespace Fenestra.Core.Tray;

public class StaticTrayIcon : TrayIconBase
{
    private readonly MemoryStream _stream;

    public StaticTrayIcon(MemoryStream stream)
    {
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("Stream must be readable and seekable.", nameof(stream));

        _stream = stream;
    }

    public override void Initialize()
    {
        var raw = NativeMethods.CreateIconFromResourceEx(
            _stream.ToArray(),
            (int)_stream.Length,
            true,
            0x00030000,
            16,
            16,
            0);

        if (raw == IntPtr.Zero)
            throw new InvalidOperationException(
                $"CreateIconFromResourceEx failed with error {Marshal.GetLastWin32Error()}.");

        UpdateHandle(new SafeIconHandle(raw, ownsHandle: true));
    }
}