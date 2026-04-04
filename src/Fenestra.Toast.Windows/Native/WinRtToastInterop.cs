using System.Runtime.InteropServices;

namespace Fenestra.Toast.Windows.Native;

/// <summary>
/// Pure P/Invoke and vtable-level WinRT COM interop. Everything is IntPtr — no COM interface casts.
/// Uses GetDelegateForFunctionPointer to call vtable slots directly.
/// </summary>
internal static class WinRtToastInterop
{
    // --- WinRT Activation ---

    public static IntPtr ActivateInstance(string className)
    {
        WindowsCreateString(className, className.Length, out var hClass);
        try
        {
            var hr = RoActivateInstance(hClass, out var instance);
            return hr == 0 ? instance : IntPtr.Zero;
        }
        catch { return IntPtr.Zero; }
        finally { WindowsDeleteString(hClass); }
    }

    public static IntPtr GetActivationFactory(string className, Guid iid)
    {
        WindowsCreateString(className, className.Length, out var hClass);
        try
        {
            var hr = RoGetActivationFactory(hClass, ref iid, out var factory);
            return hr == 0 ? factory : IntPtr.Zero;
        }
        catch { return IntPtr.Zero; }
        finally { WindowsDeleteString(hClass); }
    }

    // --- HSTRING helpers ---

    public static IntPtr CreateHString(string value)
    {
        WindowsCreateString(value, value.Length, out var h);
        return h;
    }

    public static string ReadHString(IntPtr hstring)
    {
        if (hstring == IntPtr.Zero) return "";
        var buf = WindowsGetStringRawBuffer(hstring, out var len);
        return buf == IntPtr.Zero ? "" : Marshal.PtrToStringUni(buf, len) ?? "";
    }

    public static void FreeHString(IntPtr hstring)
    {
        if (hstring != IntPtr.Zero) WindowsDeleteString(hstring);
    }

    // --- Vtable call helpers ---

    /// <summary>
    /// Gets a function pointer from a COM vtable at the given slot index.
    /// WinRT interfaces: slots 0-2 = IUnknown, 3-5 = IInspectable, 6+ = interface methods.
    /// </summary>
    public static IntPtr GetVtableSlot(IntPtr pObj, int slot)
    {
        var vtable = Marshal.ReadIntPtr(pObj);
        return Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
    }

    /// <summary>
    /// Calls a vtable method with signature: HRESULT Method(this, HSTRING arg, out IntPtr result).
    /// Used for factory methods like CreateToastNotifierWithId.
    /// </summary>
    public static IntPtr CallWithHString(IntPtr pObj, int slot, string arg)
    {
        var hArg = CreateHString(arg);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtableHStringOutPtr>(GetVtableSlot(pObj, slot));
            var hr = fn(pObj, hArg, out var result);
            return hr == 0 ? result : IntPtr.Zero;
        }
        finally { FreeHString(hArg); }
    }

    /// <summary>
    /// Calls: HRESULT Method(this, IntPtr arg) — e.g. Show(notification), Hide(notification).
    /// </summary>
    public static int CallWithPtr(IntPtr pObj, int slot, IntPtr arg)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtablePtrVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, arg);
    }

    /// <summary>
    /// Calls: HRESULT Method(this, HSTRING arg) — e.g. put_Tag, put_Group, LoadXml.
    /// </summary>
    public static int CallSetHString(IntPtr pObj, int slot, string value)
    {
        var h = CreateHString(value);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtableHStringVoid>(GetVtableSlot(pObj, slot));
            return fn(pObj, h);
        }
        finally { FreeHString(h); }
    }

    /// <summary>
    /// Calls: HRESULT Method(this, bool value) — e.g. put_SuppressPopup, put_ExpiresOnReboot.
    /// </summary>
    public static int CallSetBool(IntPtr pObj, int slot, bool value)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableBoolVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value ? 1 : 0);
    }

    /// <summary>
    /// Calls: HRESULT Method(this, int value) — e.g. put_Priority.
    /// </summary>
    public static int CallSetInt(IntPtr pObj, int slot, int value)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableIntVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value);
    }

    /// <summary>
    /// Calls: HRESULT Method(this, out IntPtr result) — e.g. get_Values, get_History.
    /// </summary>
    public static IntPtr CallGetPtr(IntPtr pObj, int slot)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableOutPtr>(GetVtableSlot(pObj, slot));
        var hr = fn(pObj, out var result);
        return hr == 0 ? result : IntPtr.Zero;
    }

    /// <summary>
    /// Calls: HRESULT Method(this, out int result).
    /// </summary>
    public static int CallGetInt(IntPtr pObj, int slot)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableOutInt>(GetVtableSlot(pObj, slot));
        fn(pObj, out var result);
        return result;
    }

    /// <summary>
    /// Calls: HRESULT Method(this, out HSTRING result) and reads the string.
    /// </summary>
    public static string CallGetHString(IntPtr pObj, int slot)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableOutPtr>(GetVtableSlot(pObj, slot));
        fn(pObj, out var hResult);
        var str = ReadHString(hResult);
        FreeHString(hResult);
        return str;
    }

    /// <summary>
    /// Calls: HRESULT Method(this, IntPtr handler, out long token) — add event.
    /// </summary>
    public static long CallAddEvent(IntPtr pObj, int slot, IntPtr handler)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableAddEvent>(GetVtableSlot(pObj, slot));
        fn(pObj, handler, out var token);
        return token;
    }

    /// <summary>
    /// Calls: HRESULT Method(this, uint value) — e.g. put_SequenceNumber.
    /// </summary>
    public static int CallSetUInt(IntPtr pObj, int slot, uint value)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableUIntVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value);
    }

    /// <summary>
    /// Calls: HRESULT Method(this, IntPtr ptr, HSTRING a, HSTRING b) — UpdateWithTagAndGroup.
    /// </summary>
    public static int CallUpdateTagGroup(IntPtr pObj, int slot, IntPtr data, string tag, string group)
    {
        var hTag = CreateHString(tag);
        var hGroup = CreateHString(group);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtablePtrHStringHString>(GetVtableSlot(pObj, slot));
            return fn(pObj, data, hTag, hGroup);
        }
        finally
        {
            FreeHString(hTag);
            FreeHString(hGroup);
        }
    }

    /// <summary>
    /// Calls: HRESULT Method(this, IntPtr ptr, HSTRING a) — UpdateWithTag.
    /// </summary>
    public static int CallUpdateTag(IntPtr pObj, int slot, IntPtr data, string tag)
    {
        var hTag = CreateHString(tag);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtablePtrHString>(GetVtableSlot(pObj, slot));
            return fn(pObj, data, hTag);
        }
        finally { FreeHString(hTag); }
    }

    /// <summary>
    /// Calls: HRESULT Method(this) — e.g. Clear().
    /// </summary>
    public static int CallVoid(IntPtr pObj, int slot)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<VtableVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj);
    }

    /// <summary>
    /// Calls: HRESULT Method(this, HSTRING a, HSTRING b) — e.g. RemoveGroupedTag.
    /// </summary>
    public static int CallHStringHString(IntPtr pObj, int slot, string a, string b)
    {
        var hA = CreateHString(a);
        var hB = CreateHString(b);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtableHStringHString>(GetVtableSlot(pObj, slot));
            return fn(pObj, hA, hB);
        }
        finally { FreeHString(hA); FreeHString(hB); }
    }

    /// <summary>
    /// Calls: HRESULT Method(this, HSTRING a, HSTRING b, HSTRING c).
    /// </summary>
    public static int CallHString3(IntPtr pObj, int slot, string a, string b, string c)
    {
        var hA = CreateHString(a);
        var hB = CreateHString(b);
        var hC = CreateHString(c);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<VtableHString3>(GetVtableSlot(pObj, slot));
            return fn(pObj, hA, hB, hC);
        }
        finally { FreeHString(hA); FreeHString(hB); FreeHString(hC); }
    }

    /// <summary>
    /// Throws a COMException if the HRESULT indicates failure.
    /// </summary>
    public static void ThrowOnFailure(int hr, string context)
    {
        if (hr < 0)
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
    }

    // Safe release
    public static void SafeRelease(ref IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.Release(ptr);
            ptr = IntPtr.Zero;
        }
    }

    // --- Delegate signatures for vtable calls ---

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableHStringOutPtr(IntPtr @this, IntPtr hstring, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtablePtrVoid(IntPtr @this, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableHStringVoid(IntPtr @this, IntPtr hstring);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableBoolVoid(IntPtr @this, int value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableIntVoid(IntPtr @this, int value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableUIntVoid(IntPtr @this, uint value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableOutPtr(IntPtr @this, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableOutInt(IntPtr @this, out int result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableAddEvent(IntPtr @this, IntPtr handler, out long token);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtablePtrHStringHString(IntPtr @this, IntPtr ptr, IntPtr h1, IntPtr h2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtablePtrHString(IntPtr @this, IntPtr ptr, IntPtr h1);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableVoid(IntPtr @this);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableHStringHString(IntPtr @this, IntPtr h1, IntPtr h2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VtableHString3(IntPtr @this, IntPtr h1, IntPtr h2, IntPtr h3);

    // --- P/Invoke ---

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(IntPtr activatableClassId, out IntPtr instance);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("combase.dll", PreserveSig = false)]
    internal static extern void WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [DllImport("combase.dll", PreserveSig = false)]
    internal static extern void WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll")]
    private static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out int length);

    [DllImport("shell32.dll", SetLastError = true)]
    internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
}
