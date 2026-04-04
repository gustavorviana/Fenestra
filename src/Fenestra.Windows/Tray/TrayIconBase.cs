using Fenestra.Core;
using Fenestra.Windows.Native;

namespace Fenestra.Windows.Tray;

/// <summary>
/// Base class for tray icon implementations with standard dispose pattern.
/// </summary>
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

    /// <inheritdoc />
    public abstract void Initialize();
}