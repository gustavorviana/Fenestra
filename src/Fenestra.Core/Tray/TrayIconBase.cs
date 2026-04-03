using Fenestra.Core.Native;

namespace Fenestra.Core.Tray;

public abstract class TrayIconBase : FenestraComponent, ITrayIcon
{
    private SafeIconHandle? _handle;

    SafeIconHandle? ITrayIcon.Handle => _handle;

    protected void UpdateHandle(IntPtr handle, bool ownsHandle = true)
        => UpdateHandle(new SafeIconHandle(handle, ownsHandle));

    protected virtual void UpdateHandle(SafeIconHandle handle)
    {
        _handle?.Dispose();
        _handle = handle;
    }

    public abstract void Initialize();
}