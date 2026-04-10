using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Minimal COM interop for <c>ITaskbarList3</c>, enough to set a taskbar overlay icon on
/// a window. Implements the base <c>ITaskbarList</c>/<c>ITaskbarList2</c> slots only as
/// stubs since we do not use them, but the vtable ordering MUST match the native layout.
/// </summary>
[ComImport]
[Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITaskbarList3
{
    // ── ITaskbarList (base) ─────────────────────────────────────────────
    [PreserveSig] int HrInit();
    [PreserveSig] int AddTab(IntPtr hwnd);
    [PreserveSig] int DeleteTab(IntPtr hwnd);
    [PreserveSig] int ActivateTab(IntPtr hwnd);
    [PreserveSig] int SetActiveAlt(IntPtr hwnd);

    // ── ITaskbarList2 ───────────────────────────────────────────────────
    [PreserveSig] int MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

    // ── ITaskbarList3 — progress slots (unused here, kept as stubs for vtable alignment) ─
    [PreserveSig] int SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
    [PreserveSig] int SetProgressState(IntPtr hwnd, int tbpFlags);
    [PreserveSig] int RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
    [PreserveSig] int UnregisterTab(IntPtr hwndTab);
    [PreserveSig] int SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
    [PreserveSig] int SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
    [PreserveSig] int ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButtons);
    [PreserveSig] int ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButtons);
    [PreserveSig] int ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
    [PreserveSig] int SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string? pszDescription);
    [PreserveSig] int SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string? pszTip);
    [PreserveSig] int SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
}

/// <summary>
/// Factory for the <c>CLSID_TaskbarList</c> COM object exposing <see cref="ITaskbarList3"/>.
/// </summary>
internal static class TaskbarListFactory
{
    // CLSID_TaskbarList from ShObjIdl.h
    private static readonly Guid CLSID_TaskbarList = new("56FDF344-FD6D-11D0-958A-006097C9A090");

    /// <summary>
    /// Creates an initialized <see cref="ITaskbarList3"/> instance. Returns <c>null</c> on
    /// pre-Windows 7 systems or when the COM object cannot be created — callers must treat
    /// the taskbar overlay API as best-effort.
    /// </summary>
    public static ITaskbarList3? TryCreate()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(CLSID_TaskbarList);
            if (type is null) return null;

            var instance = Activator.CreateInstance(type) as ITaskbarList3;
            if (instance is null) return null;

            var hr = instance.HrInit();
            if (hr < 0)
            {
                Marshal.FinalReleaseComObject(instance);
                return null;
            }

            return instance;
        }
        catch
        {
            return null;
        }
    }
}
