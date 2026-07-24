using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// <c>SHGetFileInfo</c> interop for resolving a file type's display name from an
/// extension alone (no physical file required).
/// </summary>
internal static class ShellFileInfoNative
{
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    private const uint SHGFI_TYPENAME = 0x00000400;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    /// <summary>
    /// Returns the shell type name for an extension (e.g. <c>".txt"</c> -&gt;
    /// "Text Document"), or an empty string if the shell has no description.
    /// </summary>
    /// <remarks>
    /// Only <c>SHGFI_TYPENAME</c> is requested (with <c>SHGFI_USEFILEATTRIBUTES</c>
    /// so no real file is touched). No icon is requested, so no <c>hIcon</c>
    /// handle is returned and there is nothing to free.
    /// </remarks>
    internal static string GetTypeName(string extensionWithDot)
    {
        var info = new SHFILEINFO();
        SHGetFileInfo(
            extensionWithDot,
            FILE_ATTRIBUTE_NORMAL,
            ref info,
            (uint)Marshal.SizeOf(info),
            SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);

        return info.szTypeName ?? string.Empty;
    }
}
