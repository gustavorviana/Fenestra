using Fenestra.Core.Drawing;
using Fenestra.Core.Models;
using Fenestra.Core.Tray;

namespace Fenestra.Core;

/// <summary>
/// Manages the system tray (notification area) icon.
/// </summary>
public interface ITrayIconService : IDisposable
{
    void Show();
    void Hide();
    void SetIcon(ITrayIcon icon);
    void SetTooltip(string text);
    void ShowBalloonTip(string title, string text, TrayBalloonIcon icon = TrayBalloonIcon.None, int timeoutMs = 5000);
    void SetContextMenu(IEnumerable<TrayMenuItem> items);

    void SetOverlay(ITrayIconOverlay overlay);

    /// <summary>
    /// Background color for the context menu (hex string like "#FF0000" or named color).
    /// Overrides the theme background when set. Set to null to use the theme default.
    /// </summary>
    FenestralColor? MenuBackground { get; set; }

    /// <summary>
    /// Corner radius for the context menu. Set to 0 for sharp corners (default).
    /// </summary>
    double MenuCornerRadius { get; set; }

    /// <summary>
    /// Theme for the context menu. When set to System, follows the Windows dark/light mode setting.
    /// </summary>
    TrayMenuTheme MenuTheme { get; set; }

    // ---------------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------------

    event EventHandler? Click;
    event EventHandler? DoubleClick;
    event EventHandler? BalloonTipClicked;
}