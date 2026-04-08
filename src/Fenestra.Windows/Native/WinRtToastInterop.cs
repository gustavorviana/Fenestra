using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// WinRT activation helpers and COM pointer conversion utilities.
/// All methods returning <see cref="ComRef{T}"/> transfer ownership — caller must <c>using</c> or <c>Dispose</c>.
/// </summary>
internal static class WinRtToastInterop
{
    /// <summary>Activates a WinRT class and returns an RCW wrapped in <see cref="ComRef{T}"/>.</summary>
    public static ComRef<T>? ActivateInstance<T>(string className) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoActivateInstance(hClass, out var instance);
        if (hr != 0 || instance == IntPtr.Zero) return null;
        try { return new ComRef<T>((T)Marshal.GetObjectForIUnknown(instance)); }
        catch { return null; }
        finally { Marshal.Release(instance); }
    }

    /// <summary>Gets an activation factory RCW wrapped in <see cref="ComRef{T}"/>.</summary>
    public static ComRef<T>? GetActivationFactory<T>(string className, Guid iid) where T : class
    {
        using var hClass = HStringHandle.Create(className);
        var hr = RoGetActivationFactory(hClass, ref iid, out var factory);
        if (hr != 0 || factory == IntPtr.Zero) return null;
        try { return new ComRef<T>((T)Marshal.GetObjectForIUnknown(factory)); }
        catch { return null; }
        finally { Marshal.Release(factory); }
    }

    /// <summary>
    /// Takes ownership of a COM out-parameter pointer, wraps in RCW + <see cref="ComRef{T}"/>,
    /// and releases the original pointer. Caller must <c>Dispose</c> the returned ref.
    /// </summary>
    public static ComRef<T>? CastPointer<T>(IntPtr pUnk) where T : class
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

    /// <summary>
    /// Wraps a borrowed COM pointer (one you don't own) in an RCW + <see cref="ComRef{T}"/>.
    /// AddRefs first so <see cref="CastPointer{T}"/>'s Release is balanced.
    /// </summary>
    public static ComRef<T>? BorrowPointer<T>(IntPtr pUnk) where T : class
    {
        if (pUnk == IntPtr.Zero) return null;
        Marshal.AddRef(pUnk);
        return CastPointer<T>(pUnk);
    }

    // --- P/Invoke ---

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(HStringHandle activatableClassId, out IntPtr instance);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(HStringHandle activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("shell32.dll", SetLastError = true)]
    internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
}
