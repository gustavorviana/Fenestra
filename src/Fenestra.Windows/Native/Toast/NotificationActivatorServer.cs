using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Registers a COM class factory for <c>INotificationActivationCallback</c> so Windows can
/// activate the application when a toast notification is clicked.
/// Uses CLR-generated CCWs (COM Callable Wrappers) via <see cref="IClassFactory"/> and
/// <see cref="INotificationActivationCallback"/> [ComImport] interfaces.
/// </summary>
internal static class NotificationActivatorServer
{
    private static uint _cookie;
    private static IntPtr _factoryPtr;
    private static ManagedClassFactory? _factory;

    public static void Register(Guid clsid, Action<string, string> onActivated)
    {
        if (_cookie != 0) return;

        _factory = new ManagedClassFactory(onActivated);
        _factoryPtr = Marshal.GetComInterfaceForObject(_factory, typeof(IClassFactory));

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
            Marshal.Release(_factoryPtr);
            _factoryPtr = IntPtr.Zero;
        }
        _factory = null;
    }

    // ── Managed IClassFactory implementation ────────────────────────

    private sealed class ManagedClassFactory : IClassFactory
    {
        private readonly Action<string, string> _onActivated;

        public ManagedClassFactory(Action<string, string> onActivated) => _onActivated = onActivated;

        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] CreateInstance riid={riid}");
            var activator = new ManagedActivator(_onActivated);
            var pUnk = Marshal.GetIUnknownForObject(activator);
            var hr = Marshal.QueryInterface(pUnk, ref riid, out ppvObject);
            Marshal.Release(pUnk);
            return hr;
        }

        public int LockServer(bool fLock) => 0;
    }

    // ── Managed INotificationActivationCallback implementation ─────

    private sealed class ManagedActivator : INotificationActivationCallback
    {
        private readonly Action<string, string> _onActivated;

        public ManagedActivator(Action<string, string> onActivated) => _onActivated = onActivated;

        public int Activate(string appUserModelId, string invokedArgs, IntPtr data, uint count)
        {
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] Activate called!");
            try
            {
                _onActivated(appUserModelId ?? "", invokedArgs ?? "");
                return 0;
            }
            catch
            {
                return unchecked((int)0x80004005); // E_FAIL
            }
        }
    }

    // ── P/Invoke ──────────────────────────────────────���─────────────

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
