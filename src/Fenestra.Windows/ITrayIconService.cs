using Fenestra.Windows.Models;
using Fenestra.Windows.Tray;

namespace Fenestra.Windows;

/// <summary>
/// Manages the system tray (notification area) icon.
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Shows the tray icon in the notification area.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the tray icon from the notification area.
    /// </summary>
    void Hide();

    /// <summary>
    /// Sets the icon displayed in the system tray.
    /// </summary>
    void SetIcon(ITrayIcon icon);

    /// <summary>
    /// Sets the tooltip text shown when hovering over the tray icon.
    /// </summary>
    void SetTooltip(string text);

    /// <summary>
    /// Displays a balloon notification above the tray icon.
    /// </summary>
    void ShowBalloonTip(string title, string text, TrayBalloonIcon icon = TrayBalloonIcon.None, int timeoutMs = 5000);

    /// <summary>
    /// Sets the context menu items shown on right-click.
    /// </summary>
    void SetContextMenu(IEnumerable<TrayMenuItem> items);

    /// <summary>
    /// Attaches an overlay that composites on top of the tray icon.
    /// </summary>
    void SetOverlay(ITrayIconOverlay overlay);

    /// <summary>
    /// Style settings for the context menu (theme, background, corner radius).
    /// </summary>
    TrayMenuStyle? MenuStyle { get; }

    /// <summary>
    /// Raised when the tray icon is single-clicked.
    /// </summary>
    event EventHandler? Click;

    /// <summary>
    /// Raised when the tray icon is double-clicked.
    /// </summary>
    event EventHandler? DoubleClick;

    /// <summary>
    /// Raised when the user clicks on a balloon notification.
    /// </summary>
    event EventHandler? BalloonTipClicked;
}
