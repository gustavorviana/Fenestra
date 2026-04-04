using System.Runtime.InteropServices;
using System.Threading;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Creates COM-compatible TypedEventHandler objects for WinRT toast notification events.
/// Builds a manual vtable (IUnknown + Invoke) in unmanaged memory so WinRT can call back into managed code.
/// </summary>
internal static class TypedEventHandlerFactory
{
    // Shared vtable — allocated once, never freed.
    // Layout: [QueryInterface][AddRef][Release][Invoke]
    private static readonly IntPtr s_vtable;

    // Static delegate instances + GCHandles to prevent GC collection.
    private static readonly QueryInterfaceFn s_qi;
    private static readonly AddRefFn s_addRef;
    private static readonly ReleaseFn s_release;
    private static readonly InvokeFn s_invoke;

#pragma warning disable IDE0052 // prevent GC of pinned delegates
    private static readonly GCHandle s_qiPin, s_addRefPin, s_releasePin, s_invokePin;
#pragma warning restore IDE0052

    static TypedEventHandlerFactory()
    {
        s_qi = QueryInterfaceImpl;
        s_addRef = AddRefImpl;
        s_release = ReleaseImpl;
        s_invoke = InvokeImpl;

        s_qiPin = GCHandle.Alloc(s_qi);
        s_addRefPin = GCHandle.Alloc(s_addRef);
        s_releasePin = GCHandle.Alloc(s_release);
        s_invokePin = GCHandle.Alloc(s_invoke);

        s_vtable = Marshal.AllocHGlobal(4 * IntPtr.Size);
        Marshal.WriteIntPtr(s_vtable, 0 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_qi));
        Marshal.WriteIntPtr(s_vtable, 1 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_addRef));
        Marshal.WriteIntPtr(s_vtable, 2 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_release));
        Marshal.WriteIntPtr(s_vtable, 3 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_invoke));
    }

    /// <summary>
    /// Creates a COM event handler that calls <paramref name="callback"/> when Invoke is called.
    /// The returned IntPtr is a COM object with refCount=1. The caller owns this reference.
    /// </summary>
    public static IntPtr Create(Guid iid, Action<IntPtr, IntPtr> callback)
    {
        var state = new HandlerState(iid, callback);
        var gcHandle = GCHandle.Alloc(state);

        // Object layout in unmanaged memory: [pVtable][pGCHandle]
        var pObj = Marshal.AllocHGlobal(2 * IntPtr.Size);
        Marshal.WriteIntPtr(pObj, 0, s_vtable);
        Marshal.WriteIntPtr(pObj, IntPtr.Size, GCHandle.ToIntPtr(gcHandle));

        return pObj;
    }

    /// <summary>
    /// Releases the caller's reference to a handler created by <see cref="Create"/>.
    /// </summary>
    public static void Release(IntPtr handler)
    {
        if (handler != IntPtr.Zero)
            ReleaseImpl(handler);
    }

    // --- Internal state ---

    private sealed class HandlerState
    {
        public readonly Guid Iid;
        public readonly Action<IntPtr, IntPtr> Callback;
        public int RefCount = 1;

        public HandlerState(Guid iid, Action<IntPtr, IntPtr> callback)
        {
            Iid = iid;
            Callback = callback;
        }
    }

    private static HandlerState GetState(IntPtr pObj)
    {
        var ptr = Marshal.ReadIntPtr(pObj, IntPtr.Size);
        return (HandlerState)GCHandle.FromIntPtr(ptr).Target!;
    }

    // --- IUnknown + Invoke implementation ---

    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");

    private static int QueryInterfaceImpl(IntPtr @this, ref Guid iid, out IntPtr ppv)
    {
        var state = GetState(@this);
        if (iid == IID_IUnknown || iid == state.Iid)
        {
            ppv = @this;
            Interlocked.Increment(ref state.RefCount);
            return 0; // S_OK
        }
        ppv = IntPtr.Zero;
        return unchecked((int)0x80004002); // E_NOINTERFACE
    }

    private static uint AddRefImpl(IntPtr @this)
    {
        return (uint)Interlocked.Increment(ref GetState(@this).RefCount);
    }

    private static uint ReleaseImpl(IntPtr @this)
    {
        var state = GetState(@this);
        var count = Interlocked.Decrement(ref state.RefCount);
        if (count <= 0)
        {
            var ptr = Marshal.ReadIntPtr(@this, IntPtr.Size);
            GCHandle.FromIntPtr(ptr).Free();
            Marshal.FreeHGlobal(@this);
        }
        return (uint)Math.Max(count, 0);
    }

    private static int InvokeImpl(IntPtr @this, IntPtr sender, IntPtr args)
    {
        try
        {
            GetState(@this).Callback(sender, args);
            return 0; // S_OK
        }
        catch
        {
            return unchecked((int)0x80004005); // E_FAIL
        }
    }

    // --- Delegate signatures for vtable ---

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int QueryInterfaceFn(IntPtr @this, ref Guid iid, out IntPtr ppv);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint AddRefFn(IntPtr @this);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseFn(IntPtr @this);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int InvokeFn(IntPtr @this, IntPtr sender, IntPtr args);
}
