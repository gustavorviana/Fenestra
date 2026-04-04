using System.Runtime.InteropServices;

namespace Fenestra.Wpf.Native;

internal class SafeMenuHandle : SafeHandle
{
    public SafeMenuHandle() : base(IntPtr.Zero, true) { }

    public SafeMenuHandle(IntPtr handle) : base(IntPtr.Zero, true)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.DestroyMenu(handle);
    }
}
