using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Custom marshaler for WinRT HSTRING ↔ managed <see cref="string"/> conversion.
/// Works on both .NET Framework 4.7.2 and .NET 6+ (unlike <c>UnmanagedType.HString</c>
/// which was removed from the .NET 6 runtime).
/// <para>
/// Usage: <c>[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(HStringMarshaler))]</c>
/// </para>
/// </summary>
internal sealed class HStringMarshaler : ICustomMarshaler
{
    private static readonly HStringMarshaler Instance = new();

    // Required by the CLR — must be a static method named GetInstance.
    public static ICustomMarshaler GetInstance(string cookie) => Instance;

    public int GetNativeDataSize() => IntPtr.Size;

    public IntPtr MarshalManagedToNative(object managedObj)
    {
        if (managedObj is not string str || str.Length == 0)
            return IntPtr.Zero;

        WindowsCreateString(str, str.Length, out var hstring);
        return hstring;
    }

    public object MarshalNativeToManaged(IntPtr pNativeData)
    {
        if (pNativeData == IntPtr.Zero) return "";
        var buf = WindowsGetStringRawBuffer(pNativeData, out var len);
        return buf == IntPtr.Zero ? "" : Marshal.PtrToStringUni(buf, len) ?? "";
    }

    public void CleanUpManagedData(object managedObj) { }

    public void CleanUpNativeData(IntPtr pNativeData)
    {
        if (pNativeData != IntPtr.Zero)
            WindowsDeleteString(pNativeData);
    }

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = false)]
    private static extern void WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll")]
    private static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out int length);
}
