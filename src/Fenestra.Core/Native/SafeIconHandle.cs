using System.Runtime.InteropServices;

namespace Fenestra.Core.Native;

public sealed class SafeIconHandle : SafeHandle
{
    public SafeIconHandle() : base(IntPtr.Zero, true) { }

    public SafeIconHandle(IntPtr handle, bool ownsHandle = true) : base(IntPtr.Zero, ownsHandle)
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.DestroyIcon(handle);
    }
}