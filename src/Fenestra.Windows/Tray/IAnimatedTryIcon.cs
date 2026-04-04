namespace Fenestra.Windows.Tray;

/// <summary>
/// Animated tray icon that cycles through frames.
/// </summary>
public interface IAnimatedTryIcon : ITrayIcon
{
    /// <summary>
    /// Raised when the current animation frame changes.
    /// </summary>
    event EventHandler OnIconChanged;

    /// <summary>
    /// Starts animating the tray icon by cycling through the provided frames.
    /// </summary>
    void StartIconAnimation();

    /// <summary>
    /// Stops the animation and restores the original icon.
    /// </summary>
    void StopIconAnimation();

    /// <summary>
    /// True if the icon is currently animating.
    /// </summary>
    bool IsAnimating { get; }
}