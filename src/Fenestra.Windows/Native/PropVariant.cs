using Fenestra.Windows.Native.Structs;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

internal sealed class PropVariant : IDisposable
{
    private PROPVARIANT _value;
    private bool _disposed;

    public PropVariant(string value)
    {
        _value = new PROPVARIANT
        {
            vt = 31,
            p = Marshal.StringToCoTaskMemUni(value)
        };
    }

    public static implicit operator PROPVARIANT(PropVariant value)
    {
        return value._value;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        PropVariantClear(ref _value);
        _disposed = true;
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);
}
