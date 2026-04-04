using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Fenestra.Toast.Windows.Native;

/// <summary>
/// SafeHandle wrapper for COM IUnknown pointers. Automatically calls Marshal.Release on dispose.
/// </summary>
internal sealed class ComPointerHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public ComPointerHandle() : base(true) { }

    public ComPointerHandle(IntPtr ptr) : base(true)
    {
        SetHandle(ptr);
    }

    protected override bool ReleaseHandle()
    {
        Marshal.Release(handle);
        return true;
    }

    /// <summary>
    /// Wraps a raw COM pointer. Takes ownership — the pointer will be released on dispose.
    /// </summary>
    public static ComPointerHandle Wrap(IntPtr ptr)
    {
        return new ComPointerHandle(ptr);
    }

    /// <summary>
    /// QueryInterface on the underlying pointer. The returned handle owns the new reference.
    /// Returns null if QI fails.
    /// </summary>
    public ComPointerHandle? QueryInterface(Guid iid)
    {
        if (IsInvalid) return null;
        var hr = Marshal.QueryInterface(handle, ref iid, out var result);
        return hr == 0 && result != IntPtr.Zero ? new ComPointerHandle(result) : null;
    }
}
