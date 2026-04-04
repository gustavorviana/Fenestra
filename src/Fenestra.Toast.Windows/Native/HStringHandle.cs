using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Fenestra.Toast.Windows.Native;

/// <summary>
/// SafeHandle wrapper for Windows Runtime HSTRING. Automatically freed via WindowsDeleteString on dispose.
/// </summary>
internal sealed class HStringHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public HStringHandle() : base(true) { }

    /// <summary>
    /// Wraps an existing HSTRING pointer. Takes ownership.
    /// </summary>
    public HStringHandle(IntPtr existingHandle) : base(true)
    {
        SetHandle(existingHandle);
    }

    protected override bool ReleaseHandle()
    {
        WindowsDeleteString(handle);
        return true;
    }

    /// <summary>
    /// Creates an HSTRING from a managed string.
    /// </summary>
    public static HStringHandle Create(string value)
    {
        var h = new HStringHandle();
        WindowsCreateString(value, value.Length, out var ptr);
        h.SetHandle(ptr);
        return h;
    }

    /// <summary>
    /// Reads the HSTRING value back to a managed string.
    /// </summary>
    public override string ToString()
    {
        if (IsInvalid) return "";
        var buf = WindowsGetStringRawBuffer(handle, out var len);
        return buf == IntPtr.Zero ? "" : Marshal.PtrToStringUni(buf, len) ?? "";
    }

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll")]
    private static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out int length);
}
