using System.Runtime.InteropServices;
using static Fenestra.Toast.Windows.Native.Toast.ToastInteropConstants;

namespace Fenestra.Toast.Windows.Native.Toast;

/// <summary>
/// Responsible for creating and displaying toast notifications (Show/Hide).
/// Owns the IToastNotifier COM pointer.
/// </summary>
internal sealed class NativeToastDisplay : IDisposable
{
    private readonly ComPointerHandle _pToastNotifier;

    public NativeToastDisplay(ComPointerHandle pNotifier)
    {
        _pToastNotifier = pNotifier.QueryInterface(IID_IToastNotifier)
            ?? throw new InvalidOperationException("QI for IToastNotifier failed.");
    }

    public void Show(ComPointerHandle pNotification)
    {
        using var pNotif = pNotification.QueryInterface(IID_IToastNotification);
        if (pNotif == null)
            throw new InvalidOperationException("QI for IToastNotification failed.");

        var hr = WinRtToastInterop.CallWithPtr(_pToastNotifier, Slot_Notifier_Show, pNotif.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Show failed. HRESULT=0x{hr:X8}", hr);
    }

    public void Hide(ComPointerHandle pNotification)
    {
        using var pNotif = pNotification.QueryInterface(IID_IToastNotification);
        if (pNotif == null)
            throw new InvalidOperationException("QI for IToastNotification failed.");

        var hr = WinRtToastInterop.CallWithPtr(_pToastNotifier, Slot_Notifier_Hide, pNotif.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Hide failed. HRESULT=0x{hr:X8}", hr);
    }

    public void Dispose() => _pToastNotifier.Dispose();
}
