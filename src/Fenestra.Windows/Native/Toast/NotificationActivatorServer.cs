using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Registers a COM class factory for <c>INotificationActivationCallback</c> so Windows can
/// activate the application when a toast notification is clicked while the app is closed.
/// Windows launches the EXE via LocalServer32, then calls the registered class factory
/// to create an activation callback instance.
/// </summary>
internal static class NotificationActivatorServer
{
    private static uint _cookie;

    /// <summary>
    /// Registers a COM class factory for the given CLSID. When Windows activates a toast,
    /// it creates an instance via this factory and calls <c>INotificationActivationCallback.Activate</c>,
    /// which in turn invokes <paramref name="onActivated"/>.
    /// </summary>
    public static void Register(Guid clsid, Action<string, string> onActivated)
    {
        if (_cookie != 0) return;

        var factory = new ActivatorClassFactory(onActivated);
        var hr = CoRegisterClassObject(
            ref clsid,
            factory,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out _cookie);

        if (hr < 0)
        {
            _cookie = 0;
            System.Diagnostics.Debug.WriteLine($"[Fenestra.Toast] CoRegisterClassObject failed: 0x{hr:X8}");
        }
    }

    /// <summary>
    /// Revokes the COM class factory registration.
    /// </summary>
    public static void Unregister()
    {
        if (_cookie != 0)
        {
            CoRevokeClassObject(_cookie);
            _cookie = 0;
        }
    }

    // --- COM interfaces ---

    [ComImport, Guid("00000001-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

        [PreserveSig]
        int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }

    [ComImport, Guid("53E31837-6600-4A81-9395-75CFFE746B32"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface INotificationActivationCallback
    {
        void Activate(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
            IntPtr data,
            uint count);
    }

    // --- Implementations ---

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private class ActivatorClassFactory : IClassFactory
    {
        private readonly Action<string, string> _onActivated;

        public ActivatorClassFactory(Action<string, string> onActivated)
            => _onActivated = onActivated;

        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            var activator = new NotificationActivator(_onActivated);
            ppvObject = Marshal.GetComInterfaceForObject(activator, typeof(INotificationActivationCallback));
            return 0; // S_OK
        }

        public int LockServer(bool fLock) => 0; // S_OK
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private class NotificationActivator : INotificationActivationCallback
    {
        private readonly Action<string, string> _onActivated;

        public NotificationActivator(Action<string, string> onActivated)
            => _onActivated = onActivated;

        public void Activate(string appUserModelId, string invokedArgs, IntPtr data, uint count)
        {
            try { _onActivated(appUserModelId, invokedArgs); }
            catch { }
        }
    }

    // --- P/Invoke ---

    private const uint CLSCTX_LOCAL_SERVER = 4;
    private const uint REGCLS_MULTIPLEUSE = 1;

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        ref Guid rclsid,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}
