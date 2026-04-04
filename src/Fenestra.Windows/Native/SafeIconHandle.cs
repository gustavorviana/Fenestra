using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Safe handle wrapper for Windows HICON resources.
/// </summary>
public sealed class SafeIconHandle : SafeHandle
{
    /// <summary>
    /// Initializes a new invalid icon handle.
    /// </summary>
    public SafeIconHandle() : base(IntPtr.Zero, true) { }

    /// <summary>
    /// Initializes a new instance wrapping the specified HICON pointer.
    /// </summary>
    public SafeIconHandle(IntPtr handle, bool ownsHandle = true) : base(IntPtr.Zero, ownsHandle)
    {
        SetHandle(handle);
    }

    /// <inheritdoc />
    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return ShellNativeMethods.DestroyIcon(handle);
    }
}
