using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// Pure P/Invoke and vtable-level WinRT COM interop.
/// Public API uses <see cref="ComPointerHandle"/> and <see cref="HStringHandle"/> for safe lifecycle.
/// Vtable delegates use raw IntPtr — SafeHandle cannot be used in native function pointer delegates.
/// </summary>
internal static class WinRtToastInterop
{
    // --- WinRT Activation ---

    /// <summary>Activates a WinRT class by name. Caller owns the returned handle.</summary>
    public static ComPointerHandle? ActivateInstance(string className)
    {
        using var hClass = HStringHandle.Create(className);
        try
        {
            var hr = RoActivateInstance(hClass, out var instance);
            return hr == 0 && instance != IntPtr.Zero ? new ComPointerHandle(instance) : null;
        }
        catch { return null; }
    }

    /// <summary>Gets an activation factory for a WinRT class. Caller owns the returned handle.</summary>
    public static ComPointerHandle? GetActivationFactory(string className, Guid iid)
    {
        using var hClass = HStringHandle.Create(className);
        try
        {
            var hr = RoGetActivationFactory(hClass, ref iid, out var factory);
            return hr == 0 && factory != IntPtr.Zero ? new ComPointerHandle(factory) : null;
        }
        catch { return null; }
    }

    // --- Vtable slot resolution ---

    /// <summary>
    /// Gets a raw function pointer from a COM vtable at the given slot index.
    /// WinRT: slots 0-2 = IUnknown, 3-5 = IInspectable, 6+ = interface methods.
    /// Returns IntPtr (not a COM object — do NOT wrap in ComPointerHandle).
    /// </summary>
    public static IntPtr GetVtableSlot(ComPointerHandle pObj, int slot)
    {
        if (pObj == null || pObj.IsInvalid)
            throw new ArgumentException("COM object pointer is null.", nameof(pObj));

        var vtable = Marshal.ReadIntPtr(pObj.DangerousGetHandle());
        return Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
    }

    // --- Vtable call helpers (accept ComPointerHandle, extract IntPtr internally) ---

    /// <summary>HRESULT Method(this, HSTRING, out IntPtr) — e.g. CreateToastNotifierWithId.</summary>
    public static ComPointerHandle? CallWithHString(ComPointerHandle pObj, int slot, string arg)
    {
        var p = pObj;
        using var hArg = HStringHandle.Create(arg);
        var fn = ComFactory.GetDelegate<D_HStringOutPtr>(GetVtableSlot(p, slot));
        var hr = fn(p, hArg, out var result);
        return hr == 0 && result != IntPtr.Zero ? new ComPointerHandle(result) : null;
    }

    /// <summary>HRESULT Method(this, IntPtr) — e.g. Show, Hide.</summary>
    public static int CallWithPtr(ComPointerHandle pObj, int slot, IntPtr arg)
    {
        var fn = ComFactory.GetDelegate<D_PtrVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, arg);
    }

    /// <summary>HRESULT Method(this, HSTRING) — e.g. put_Tag, LoadXml.</summary>
    public static int CallSetHString(ComPointerHandle pObj, int slot, string value)
    {
        using var h = HStringHandle.Create(value);
        var fn = ComFactory.GetDelegate<D_HStringVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, h);
    }

    /// <summary>HRESULT Method(this, bool).</summary>
    public static int CallSetBool(ComPointerHandle pObj, int slot, bool value)
    {
        var fn = ComFactory.GetDelegate<D_IntVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value ? 1 : 0);
    }

    /// <summary>HRESULT Method(this, int).</summary>
    public static int CallSetInt(ComPointerHandle pObj, int slot, int value)
    {
        var fn = ComFactory.GetDelegate<D_IntVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value);
    }

    /// <summary>HRESULT Method(this, uint).</summary>
    public static int CallSetUInt(ComPointerHandle pObj, int slot, uint value)
    {
        var fn = ComFactory.GetDelegate<D_UIntVoid>(GetVtableSlot(pObj, slot));
        return fn(pObj, value);
    }

    /// <summary>HRESULT Method(this, out IntPtr) — e.g. get_Values, get_History.</summary>
    public static ComPointerHandle? CallGetPtr(ComPointerHandle pObj, int slot)
    {
        var fn = ComFactory.GetDelegate<D_OutPtr>(GetVtableSlot(pObj, slot));
        var hr = fn(pObj, out var result);
        return hr == 0 && result != IntPtr.Zero ? new ComPointerHandle(result) : null;
    }

    /// <summary>HRESULT Method(this, out int).</summary>
    public static int CallGetInt(ComPointerHandle pObj, int slot)
    {
        var fn = ComFactory.GetDelegate<D_OutInt>(GetVtableSlot(pObj, slot));
        fn(pObj, out var result);
        return result;
    }

    /// <summary>HRESULT Method(this, IntPtr handler, out long token) — add event.</summary>
    public static long CallAddEvent(ComPointerHandle pObj, int slot, IntPtr handler)
    {
        var fn = ComFactory.GetDelegate<D_AddEvent>(GetVtableSlot(pObj, slot));
        fn(pObj, handler, out var token);
        return token;
    }

    /// <summary>HRESULT Method(this, IntPtr, HSTRING, HSTRING) — UpdateWithTagAndGroup.</summary>
    public static int CallUpdateTagGroup(ComPointerHandle pObj, int slot, ComPointerHandle data, string tag, string group)
    {
        using var hTag = HStringHandle.Create(tag);
        using var hGroup = HStringHandle.Create(group);
        var fn = ComFactory.GetDelegate<D_PtrHStringHString>(GetVtableSlot(pObj, slot));
        return fn(pObj, data, hTag, hGroup);
    }

    /// <summary>HRESULT Method(this, IntPtr, HSTRING) — UpdateWithTag.</summary>
    public static int CallUpdateTag(ComPointerHandle pObj, int slot, ComPointerHandle data, string tag)
    {
        using var hTag = HStringHandle.Create(tag);
        var fn = ComFactory.GetDelegate<D_PtrHString>(GetVtableSlot(pObj, slot));
        return fn(pObj, data, hTag);
    }

    /// <summary>HRESULT Method(this) — e.g. Clear().</summary>
    public static int CallVoid(ComPointerHandle pObj, int slot)
    {
        var fn = ComFactory.GetDelegate<D_Void>(GetVtableSlot(pObj, slot));
        return fn(pObj);
    }

    /// <summary>HRESULT Method(this, HSTRING, HSTRING).</summary>
    public static int CallHStringHString(ComPointerHandle pObj, int slot, string a, string b)
    {
        using var hA = HStringHandle.Create(a);
        using var hB = HStringHandle.Create(b);
        var fn = ComFactory.GetDelegate<D_HStringHString>(GetVtableSlot(pObj, slot));
        return fn(pObj, hA, hB);
    }

    // --- Native vtable delegate signatures (ALL IntPtr — SafeHandle not allowed here) ---

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_HStringOutPtr(ComPointerHandle @this, HStringHandle hstring, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_PtrVoid(ComPointerHandle @this, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_HStringVoid(ComPointerHandle @this, HStringHandle hstring);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_IntVoid(ComPointerHandle @this, int value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_UIntVoid(ComPointerHandle @this, uint value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_OutPtr(ComPointerHandle @this, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_OutInt(ComPointerHandle @this, out int result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_AddEvent(ComPointerHandle @this, IntPtr handler, out long token);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_PtrHStringHString(ComPointerHandle @this, ComPointerHandle ptr, HStringHandle h1, HStringHandle h2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_PtrHString(ComPointerHandle @this, ComPointerHandle ptr, HStringHandle h1);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_Void(ComPointerHandle @this);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_HStringHString(ComPointerHandle @this, HStringHandle h1, HStringHandle h2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int D_HString3(ComPointerHandle @this, HStringHandle h1, IntPtr h2, IntPtr h3);

    // --- P/Invoke ---

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(HStringHandle activatableClassId, out IntPtr instance);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(HStringHandle activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("shell32.dll", SetLastError = true)]
    internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
}
