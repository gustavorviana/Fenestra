namespace Fenestra.Core.Tray;

public interface IAnimatedTryIcon : ITrayIcon
{
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