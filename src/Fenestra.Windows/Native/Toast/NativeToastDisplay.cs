using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Responsible for creating and displaying toast notifications (Show/Hide).
/// Does not own the IToastNotifier RCW — the parent <see cref="NativeToastNotifier"/> manages its lifetime.
/// </summary>
internal sealed class NativeToastDisplay
{
    private readonly IToastNotifier _notifier;

    public NativeToastDisplay(IToastNotifier notifier)
    {
        _notifier = notifier;
    }

    public void Show(ComPointerHandle pNotification)
    {
        var hr = _notifier.Show(pNotification.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Show failed. HRESULT=0x{hr:X8}", hr);
    }

    public void Hide(ComPointerHandle pNotification)
    {
        var hr = _notifier.Hide(pNotification.DangerousGetHandle());
        if (hr < 0) throw new COMException($"IToastNotifier.Hide failed. HRESULT=0x{hr:X8}", hr);
    }
}
