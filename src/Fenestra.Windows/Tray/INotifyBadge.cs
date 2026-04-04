using Fenestra.Core.Drawing;

namespace Fenestra.Windows.Tray;

/// <summary>
/// Badge overlay that displays a notification count or dot on the tray icon.
/// </summary>
public interface INotifyBadge
{
    /// <summary>
    /// Gets or sets the notification count displayed on the badge.
    /// </summary>
    int Quantity { get; set; }

    /// <summary>
    /// Gets whether the badge is displayed as a dot instead of a number.
    /// </summary>
    bool IsDot { get; }

    /// <summary>
    /// Gets or sets the badge background color.
    /// </summary>
    FenestralColor Background { get; set; }

    /// <summary>
    /// Gets or sets the badge text color.
    /// </summary>
    FenestralColor Foreground { get; set; }

    /// <summary>
    /// Switches the badge to dot mode (no number displayed).
    /// </summary>
    void SetDot();

    /// <summary>
    /// Removes the badge from the tray icon.
    /// </summary>
    void Clear();
}