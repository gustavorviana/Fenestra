using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Interfaces;

/// <summary>
/// Win32 shell API for building an application's Jump List. Used instead of WPF's
/// <c>System.Windows.Shell.JumpList</c> so the feature is available from any framework
/// (WinForms, Avalonia, console, WPF) without a WPF dependency.
/// </summary>
[ComImport]
[Guid("6332DEBF-87B5-4670-90C0-5E57B408A49E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICustomDestinationList
{
    [PreserveSig] int SetAppID([MarshalAs(UnmanagedType.LPWStr)] string pszAppID);

    [PreserveSig] int BeginList(out uint pcMaxSlots, ref Guid riid, out IntPtr ppv);

    [PreserveSig] int AppendCategory(
        [MarshalAs(UnmanagedType.LPWStr)] string pszCategory,
        [MarshalAs(UnmanagedType.Interface)] IObjectArray poa);

    [PreserveSig] int AppendKnownCategory(int category);

    [PreserveSig] int AddUserTasks([MarshalAs(UnmanagedType.Interface)] IObjectArray poa);

    [PreserveSig] int CommitList();

    [PreserveSig] int GetRemovedDestinations(ref Guid riid, out IntPtr ppv);

    [PreserveSig] int DeleteList([MarshalAs(UnmanagedType.LPWStr)] string? pszAppID);

    [PreserveSig] int AbortList();
}

/// <summary>
/// <c>KNOWNDESTCATEGORY</c> values used with <see cref="ICustomDestinationList.AppendKnownCategory"/>.
/// </summary>
internal enum KnownDestinationCategory
{
    Frequent = 1,
    Recent = 2,
}

/// <summary>
/// Immutable enumerable of COM objects. <see cref="IObjectCollection"/> derives from this
/// and is how we actually populate items.
/// </summary>
[ComImport]
[Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IObjectArray
{
    [PreserveSig] int GetCount(out uint pcObjects);

    [PreserveSig] int GetAt(
        uint uiIndex,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
}

/// <summary>
/// Mutable extension of <see cref="IObjectArray"/> used to build the list of tasks/items
/// that is then handed to <see cref="ICustomDestinationList"/>.
/// </summary>
[ComImport]
[Guid("5632B1A4-E38A-400A-928A-D4CD63230295")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IObjectCollection
{
    // ── IObjectArray members (vtable slots) ─────────────────────────────
    [PreserveSig] int GetCount(out uint pcObjects);

    [PreserveSig] int GetAt(
        uint uiIndex,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    // ── IObjectCollection members ───────────────────────────────────────
    [PreserveSig] int AddObject([MarshalAs(UnmanagedType.IUnknown)] object punk);

    [PreserveSig] int AddFromArray([MarshalAs(UnmanagedType.Interface)] IObjectArray poaSource);

    [PreserveSig] int RemoveObjectAt(uint uiIndex);

    [PreserveSig] int Clear();
}

/// <summary>
/// Well-known CLSIDs used by the Jump List COM surface.
/// </summary>
internal static class JumpListClsids
{
    /// <summary>CLSID_DestinationList — creates an <see cref="ICustomDestinationList"/>.</summary>
    public static readonly Guid DestinationList = new("77F10CF0-3DB5-4966-B520-B7C54FD35ED6");

    /// <summary>CLSID_EnumerableObjectCollection — creates an empty <see cref="IObjectCollection"/>.</summary>
    public static readonly Guid EnumerableObjectCollection = new("2D3468C1-36A7-43B6-AC24-D3F02FD9607A");

    /// <summary>CLSID_ShellLink — creates a fresh <see cref="IShellLinkW"/> backing a task.</summary>
    public static readonly Guid ShellLink = new("00021401-0000-0000-C000-000000000046");

    public static readonly Guid IID_IObjectArray = new("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9");
    public static readonly Guid IID_IObjectCollection = new("5632B1A4-E38A-400A-928A-D4CD63230295");
    public static readonly Guid IID_ICustomDestinationList = new("6332DEBF-87B5-4670-90C0-5E57B408A49E");
    public static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");
}
