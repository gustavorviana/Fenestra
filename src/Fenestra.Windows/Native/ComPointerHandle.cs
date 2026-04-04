using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

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
