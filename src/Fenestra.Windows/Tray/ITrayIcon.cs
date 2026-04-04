using Fenestra.Core;
using Fenestra.Windows.Native;

namespace Fenestra.Windows.Tray;

/// <summary>
/// Represents a tray icon with a native HICON handle.
/// </summary>
public interface ITrayIcon : IFenestraComponent
{
    /// <summary>
    /// Gets the native icon handle, or null if not yet initialized.
    /// </summary>
    SafeIconHandle? Handle { get; }

    /// <summary>
    /// Initializes the icon and creates the underlying native resource.
    /// </summary>
    void Initialize();
}