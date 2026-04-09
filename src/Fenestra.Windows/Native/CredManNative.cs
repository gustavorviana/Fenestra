using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// P/Invoke declarations for the Windows Credential Manager API (advapi32.dll).
/// Wrapped by <see cref="CredManInterop"/> into a managed surface.
/// </summary>
internal static class CredManNative
{
    internal const int CRED_TYPE_GENERIC = 1;
    internal const int CRED_PERSIST_LOCAL_MACHINE = 2;

    internal const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;       // LPWSTR
        public IntPtr Comment;          // LPWSTR
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;   // LPBYTE
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;      // LPWSTR
        public IntPtr UserName;         // LPWSTR
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredWriteW(ref CREDENTIAL credential, int flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredReadW(string targetName, int type, int reservedFlags, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredDeleteW(string targetName, int type, int flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredEnumerateW(string? filter, int flags, out int count, out IntPtr credentialsPtr);

    [DllImport("advapi32.dll", SetLastError = false)]
    internal static extern void CredFree(IntPtr buffer);
}
