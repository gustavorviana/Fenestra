namespace Fenestra.Core.Tray;

/// <summary>
/// Overlay that composites on top of the tray icon.
/// </summary>
public interface ITrayIconOverlay : IDisposable
{
    /// <summary>
    /// Raised when the overlay state changes and the icon needs to be re-rendered.
    /// </summary>
    event EventHandler OnUpdate;

    /// <summary>
    /// Composites the overlay onto the base icon and returns the resulting HICON.
    /// </summary>
    IntPtr RenderBadgedIcon(IntPtr baseHIcon);
}
