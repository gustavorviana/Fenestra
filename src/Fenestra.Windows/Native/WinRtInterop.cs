using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// WinRT activation and COM pointer conversion via P/Invoke.
/// Implements <see cref="IWinRtInterop"/> for dependency injection.
/// </summary>
internal sealed class WinRtInterop : IWinRtInterop
{
    public ComRef<T>? ActivateInstance<T>(string className) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoActivateInstance(hClass, out var instance);
        if (hr != 0 || instance == IntPtr.Zero) return null;
        try { return new ComRef<T>((T)Marshal.GetObjectForIUnknown(instance)); }
        catch { return null; }
        finally { Marshal.Release(instance); }
    }

    public ComRef<T>? GetActivationFactory<T>(string className, Guid iid) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoGetActivationFactory(hClass, ref iid, out var factory);
        if (hr != 0 || factory == IntPtr.Zero) return null;
        try { return new ComRef<T>((T)Marshal.GetObjectForIUnknown(factory)); }
        catch { return null; }
        finally { Marshal.Release(factory); }
    }

    public ComRef<T>? CastPointer<T>(IntPtr pUnk) where T : class
    {
        if (pUnk == IntPtr.Zero) return null;
        object? rcw = null;
        try
        {
            rcw = Marshal.GetObjectForIUnknown(pUnk);
            return new ComRef<T>((T)rcw);
        }
        catch
        {
            if (rcw != null) Marshal.ReleaseComObject(rcw);
            return null;
        }
        finally { Marshal.Release(pUnk); }
    }

    public ComRef<T>? BorrowPointer<T>(IntPtr pUnk) where T : class
    {
        if (pUnk == IntPtr.Zero) return null;
        Marshal.AddRef(pUnk);
        return CastPointer<T>(pUnk);
    }

    public void SetCurrentProcessExplicitAppUserModelID(string appID)
        => SetCurrentProcessExplicitAppUserModelIDNative(appID);

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(HStringHandle activatableClassId, out IntPtr instance);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(HStringHandle activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("shell32.dll", SetLastError = true, EntryPoint = "SetCurrentProcessExplicitAppUserModelID")]
    private static extern void SetCurrentProcessExplicitAppUserModelIDNative([MarshalAs(UnmanagedType.LPWStr)] string appID);
}
