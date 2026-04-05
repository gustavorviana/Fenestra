using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// WinRT activation helpers and COM pointer conversion utilities.
/// </summary>
internal static class WinRtToastInterop
{
    // --- WinRT Activation ---

    /// <summary>Activates a WinRT class and returns an RCW cast to <typeparamref name="T"/>.</summary>
    public static T? ActivateInstanceAs<T>(string className) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoActivateInstance(hClass, out var instance);
        if (hr != 0 || instance == IntPtr.Zero) return null;
        try { return (T)Marshal.GetObjectForIUnknown(instance); }
        catch { return null; }
        finally { Marshal.Release(instance); }
    }

    /// <summary>Gets an activation factory RCW cast to <typeparamref name="T"/>.</summary>
    public static T? GetActivationFactoryAs<T>(string className, Guid iid) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoGetActivationFactory(hClass, ref iid, out var factory);
        if (hr != 0 || factory == IntPtr.Zero) return null;
        try { return (T)Marshal.GetObjectForIUnknown(factory); }
        catch { return null; }
        finally { Marshal.Release(factory); }
    }

    /// <summary>
    /// Takes ownership of a COM out-parameter pointer, wraps it in an RCW, and releases the original reference.
    /// Caller must <c>Marshal.ReleaseComObject</c> the returned object when done.
    /// </summary>
    public static T? CastComPointer<T>(IntPtr pUnk) where T : class
    {
        if (pUnk == IntPtr.Zero) return null;
        object? rcw = null;
        try
        {
            rcw = Marshal.GetObjectForIUnknown(pUnk);
            return (T)rcw;
        }
        catch
        {
            if (rcw != null) Marshal.ReleaseComObject(rcw);
            return null;
        }
        finally { Marshal.Release(pUnk); }
    }

    /// <summary>
    /// Creates a temporary RCW from a borrowed COM pointer (does NOT release the original pointer).
    /// Caller must <c>Marshal.ReleaseComObject</c> the returned object when done.
    /// Returns null if the cast to <typeparamref name="T"/> fails (interface not supported).
    /// </summary>
    public static T? BorrowComPointer<T>(IntPtr pUnk) where T : class
    {
        if (pUnk == IntPtr.Zero) return null;
        object? rcw = null;
        try
        {
            rcw = Marshal.GetObjectForIUnknown(pUnk);
            return (T)rcw;
        }
        catch
        {
            if (rcw != null) Marshal.ReleaseComObject(rcw);
            return null;
        }
    }

    // --- P/Invoke ---

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(HStringHandle activatableClassId, out IntPtr instance);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(HStringHandle activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("shell32.dll", SetLastError = true)]
    internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
}
