using System.Runtime.InteropServices;
using System.Text;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// COM interop for toast notification background activation (relaunch when app is closed).
/// Handles shortcut creation, COM server registration, and activation callback.
/// </summary>
internal static class ToastActivationInterop
{
    public static void RegisterComServer(string exePath, Guid activatorClsid)
    {
        var regPath = $@"SOFTWARE\Classes\CLSID\{{{activatorClsid}}}\LocalServer32";
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regPath);
        key.SetValue(null, $"\"{exePath}\"");
    }

    public static void UnregisterComServer(Guid activatorClsid)
    {
        try
        {
            var regPath = $@"SOFTWARE\Classes\CLSID\{{{activatorClsid}}}";
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(regPath, false);
        }
        catch { }
    }

    public static void CreateShortcut(string shortcutPath, string exePath, string appId, Guid activatorClsid)
    {
        var shellLink = (IShellLinkW)new CShellLink();
        shellLink.SetPath(exePath);

        var propertyStore = (IPropertyStore)shellLink;

        // Set AUMID
        var aumidProp = new PROPVARIANT { vt = (ushort)VarEnum.VT_LPWSTR };
        aumidProp.unionmember = Marshal.StringToCoTaskMemUni(appId);
        propertyStore.SetValue(ref PROPERTYKEY.AppUserModel_ID, ref aumidProp);
        PropVariantClear(ref aumidProp);

        // Set ToastActivatorCLSID
        var clsidProp = new PROPVARIANT { vt = (ushort)VarEnum.VT_CLSID };
        var guidBytes = activatorClsid.ToByteArray();
        clsidProp.unionmember = Marshal.AllocCoTaskMem(guidBytes.Length);
        Marshal.Copy(guidBytes, 0, clsidProp.unionmember, guidBytes.Length);
        propertyStore.SetValue(ref PROPERTYKEY.AppUserModel_ToastActivatorCLSID, ref clsidProp);
        PropVariantClear(ref clsidProp);

        propertyStore.Commit();

        var persistFile = (IPersistFile)shellLink;
        persistFile.Save(shortcutPath, true);
    }

    public static void RemoveShortcut(string shortcutPath)
    {
        try
        {
            if (System.IO.File.Exists(shortcutPath))
                System.IO.File.Delete(shortcutPath);
        }
        catch { }
    }

    // --- COM interfaces ---

    [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotKey(out short wHotKey);
        void SetHotKey(short wHotKey);
        void GetShowCmd(out uint iShowCmd);
        void SetShowCmd(uint iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int iIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, Guid("0000010B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint propertyCount);
        void GetAt(uint propertyIndex, out PROPERTYKEY key);
        void GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
        void SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
        void Commit();
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
    private class CShellLink { }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;

        public static PROPERTYKEY AppUserModel_ID =
            new() { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };

        public static PROPERTYKEY AppUserModel_ToastActivatorCLSID =
            new() { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 26 };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr unionmember;
    }

    [DllImport("Ole32.dll", PreserveSig = false)]
    private static extern void PropVariantClear(ref PROPVARIANT pvar);
}
