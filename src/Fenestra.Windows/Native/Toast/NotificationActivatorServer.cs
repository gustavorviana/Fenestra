using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Registers a COM class factory for <c>INotificationActivationCallback</c> so Windows can
/// activate the application when a toast notification is clicked.
/// Uses manual COM vtables to avoid CLR CCW limitations with private interfaces.
/// </summary>
internal static class NotificationActivatorServer
{
    private static uint _cookie;
    private static IntPtr _factoryPtr;
    private static GCHandle _factoryStateHandle;

    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IID_IClassFactory = new("00000001-0000-0000-C000-000000000046");
    private static readonly Guid IID_INotificationActivationCallback = new("53E31837-6600-4A81-9395-75CFFE746F94");

    public static void Register(Guid clsid, Action<string, string> onActivated)
    {
        if (_cookie != 0) return;

        _factoryPtr = CreateClassFactory(onActivated);

        var hr = CoRegisterClassObject(
            ref clsid,
            _factoryPtr,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out _cookie);

        if (hr < 0)
        {
            ReleaseFactory();
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public static void Unregister()
    {
        if (_cookie != 0)
        {
            CoRevokeClassObject(_cookie);
            _cookie = 0;
        }
        ReleaseFactory();
    }

    private static void ReleaseFactory()
    {
        if (_factoryPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_factoryPtr);
            _factoryPtr = IntPtr.Zero;
        }
        if (_factoryStateHandle.IsAllocated)
            _factoryStateHandle.Free();
    }

    // ── IClassFactory vtable ────────────────────────────────────────

    private sealed class FactoryState
    {
        public readonly Action<string, string> OnActivated;
        public int RefCount = 1;
        public FactoryState(Action<string, string> onActivated) => OnActivated = onActivated;
    }

    private static IntPtr CreateClassFactory(Action<string, string> onActivated)
    {
        var state = new FactoryState(onActivated);
        _factoryStateHandle = GCHandle.Alloc(state);

        var vtable = Marshal.AllocHGlobal(5 * IntPtr.Size);
        Marshal.WriteIntPtr(vtable, 0 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_factoryQI));
        Marshal.WriteIntPtr(vtable, 1 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_factoryAddRef));
        Marshal.WriteIntPtr(vtable, 2 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_factoryRelease));
        Marshal.WriteIntPtr(vtable, 3 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_factoryCreateInstance));
        Marshal.WriteIntPtr(vtable, 4 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_factoryLockServer));

        var pObj = Marshal.AllocHGlobal(2 * IntPtr.Size);
        Marshal.WriteIntPtr(pObj, 0, vtable);
        Marshal.WriteIntPtr(pObj, IntPtr.Size, GCHandle.ToIntPtr(_factoryStateHandle));
        return pObj;
    }

    private static FactoryState GetFactoryState(IntPtr pObj)
        => (FactoryState)GCHandle.FromIntPtr(Marshal.ReadIntPtr(pObj, IntPtr.Size)).Target!;

    private static readonly FactoryQIFn s_factoryQI = FactoryQueryInterface;
    private static readonly AddRefReleaseFn s_factoryAddRef = FactoryAddRef;
    private static readonly AddRefReleaseFn s_factoryRelease = FactoryRelease;
    private static readonly CreateInstanceFn s_factoryCreateInstance = FactoryCreateInstance;
    private static readonly LockServerFn s_factoryLockServer = FactoryLockServer;

#pragma warning disable IDE0052
    private static readonly GCHandle s_pin1 = GCHandle.Alloc(s_factoryQI);
    private static readonly GCHandle s_pin2 = GCHandle.Alloc(s_factoryAddRef);
    private static readonly GCHandle s_pin3 = GCHandle.Alloc(s_factoryRelease);
    private static readonly GCHandle s_pin4 = GCHandle.Alloc(s_factoryCreateInstance);
    private static readonly GCHandle s_pin5 = GCHandle.Alloc(s_factoryLockServer);
#pragma warning restore IDE0052

    private static int FactoryQueryInterface(IntPtr @this, ref Guid riid, out IntPtr ppv)
    {
        if (riid == IID_IUnknown || riid == IID_IClassFactory)
        {
            ppv = @this;
            Interlocked.Increment(ref GetFactoryState(@this).RefCount);
            return 0;
        }
        ppv = IntPtr.Zero;
        return unchecked((int)0x80004002);
    }

    private static uint FactoryAddRef(IntPtr @this)
        => (uint)Interlocked.Increment(ref GetFactoryState(@this).RefCount);

    private static uint FactoryRelease(IntPtr @this)
        => (uint)Math.Max(Interlocked.Decrement(ref GetFactoryState(@this).RefCount), 0);

    private static int FactoryCreateInstance(IntPtr @this, IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] CreateInstance riid={riid}");
        ppvObject = IntPtr.Zero;
        var state = GetFactoryState(@this);
        var activator = CreateActivator(state.OnActivated);

        if (riid == IID_IUnknown || riid == IID_INotificationActivationCallback)
        {
            ppvObject = activator;
            return 0;
        }

        var hr = ActivatorQueryInterface(activator, ref riid, out ppvObject);
        ActivatorRelease(activator);
        return hr;
    }

    private static int FactoryLockServer(IntPtr @this, int fLock) => 0;

    // ── INotificationActivationCallback vtable ──────────────────────

    private sealed class ActivatorState
    {
        public readonly Action<string, string> OnActivated;
        public int RefCount = 1;
        public ActivatorState(Action<string, string> onActivated) => OnActivated = onActivated;
    }

    private static IntPtr CreateActivator(Action<string, string> onActivated)
    {
        var state = new ActivatorState(onActivated);
        var gcHandle = GCHandle.Alloc(state);

        var vtable = Marshal.AllocHGlobal(4 * IntPtr.Size);
        Marshal.WriteIntPtr(vtable, 0 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_activatorQI));
        Marshal.WriteIntPtr(vtable, 1 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_activatorAddRef));
        Marshal.WriteIntPtr(vtable, 2 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_activatorRelease));
        Marshal.WriteIntPtr(vtable, 3 * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(s_activatorActivate));

        var pObj = Marshal.AllocHGlobal(2 * IntPtr.Size);
        Marshal.WriteIntPtr(pObj, 0, vtable);
        Marshal.WriteIntPtr(pObj, IntPtr.Size, GCHandle.ToIntPtr(gcHandle));
        return pObj;
    }

    private static ActivatorState GetActivatorState(IntPtr pObj)
        => (ActivatorState)GCHandle.FromIntPtr(Marshal.ReadIntPtr(pObj, IntPtr.Size)).Target!;

    private static readonly ActivatorQIFn s_activatorQI = ActivatorQueryInterface;
    private static readonly AddRefReleaseFn s_activatorAddRef = ActivatorAddRef;
    private static readonly AddRefReleaseFn s_activatorRelease = ActivatorRelease;
    private static readonly ActivateFn s_activatorActivate = ActivatorActivate;

#pragma warning disable IDE0052
    private static readonly GCHandle s_pin6 = GCHandle.Alloc(s_activatorQI);
    private static readonly GCHandle s_pin7 = GCHandle.Alloc(s_activatorAddRef);
    private static readonly GCHandle s_pin8 = GCHandle.Alloc(s_activatorRelease);
    private static readonly GCHandle s_pin9 = GCHandle.Alloc(s_activatorActivate);
#pragma warning restore IDE0052

    private static int ActivatorQueryInterface(IntPtr @this, ref Guid riid, out IntPtr ppv)
    {
        System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] Activator QI: {riid}");
        if (riid == IID_IUnknown || riid == IID_INotificationActivationCallback)
        {
            ppv = @this;
            Interlocked.Increment(ref GetActivatorState(@this).RefCount);
            return 0;
        }
        ppv = IntPtr.Zero;
        return unchecked((int)0x80004002);
    }

    private static uint ActivatorAddRef(IntPtr @this)
        => (uint)Interlocked.Increment(ref GetActivatorState(@this).RefCount);

    private static uint ActivatorRelease(IntPtr @this)
    {
        var state = GetActivatorState(@this);
        var count = Interlocked.Decrement(ref state.RefCount);
        if (count <= 0)
        {
            var ptr = Marshal.ReadIntPtr(@this, IntPtr.Size);
            var vtable = Marshal.ReadIntPtr(@this, 0);
            GCHandle.FromIntPtr(ptr).Free();
            Marshal.FreeHGlobal(vtable);
            Marshal.FreeHGlobal(@this);
        }
        return (uint)Math.Max(count, 0);
    }

    private static int ActivatorActivate(IntPtr @this, IntPtr appUserModelId, IntPtr invokedArgs, IntPtr data, uint count)
    {
        System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] Activate called!");
        try
        {
            var appId = appUserModelId != IntPtr.Zero ? Marshal.PtrToStringUni(appUserModelId) ?? "" : "";
            var args = invokedArgs != IntPtr.Zero ? Marshal.PtrToStringUni(invokedArgs) ?? "" : "";
            GetActivatorState(@this).OnActivated(appId, args);
            return 0;
        }
        catch
        {
            return unchecked((int)0x80004005);
        }
    }

    // ── Delegate signatures ─────────────────────────────────────────

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int FactoryQIFn(IntPtr @this, ref Guid riid, out IntPtr ppv);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ActivatorQIFn(IntPtr @this, ref Guid riid, out IntPtr ppv);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint AddRefReleaseFn(IntPtr @this);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateInstanceFn(IntPtr @this, IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int LockServerFn(IntPtr @this, int fLock);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ActivateFn(IntPtr @this, IntPtr appUserModelId, IntPtr invokedArgs, IntPtr data, uint count);

    // ── P/Invoke ────────────────────────────────────────────────────

    private const uint CLSCTX_LOCAL_SERVER = 4;
    private const uint REGCLS_MULTIPLEUSE = 1;

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        ref Guid rclsid,
        IntPtr pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}
