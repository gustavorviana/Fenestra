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
    /// Style settings for the context menu (theme, background, corner radius).
    /// </summary>
    TrayMenuStyle? MenuStyle { get; }

    event EventHandler? Click;
    event EventHandler? DoubleClick;
    event EventHandler? BalloonTipClicked;
}