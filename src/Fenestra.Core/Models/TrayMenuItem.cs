using Fenestra.Core.Drawing;

namespace Fenestra.Core.Models;

/// <summary>
/// Defines a menu item for the system tray context menu.
/// </summary>
public class TrayMenuItem
{
    /// <summary>
    /// Gets or sets the display text for the menu item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the action invoked when the menu item is clicked.
    /// </summary>
    public Action? Action { get; set; }

    /// <summary>
    /// Gets or sets whether this item is rendered as a separator line.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Gets or sets whether the menu item is enabled and clickable.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the child menu items for a submenu.
    /// </summary>
    public IReadOnlyList<TrayMenuItem>? Children { get; set; }

    /// <summary>
    /// Icon source — accepts a file path (string), Stream, or platform-specific image object.
    /// </summary>
    public object? Icon { get; set; }

    /// <summary>
    /// Text color as a hex string (e.g. "#FF0000") or named color (e.g. "Red").
    /// </summary>
    public FenestralColor? Foreground { get; set; }

    /// <summary>
    /// Background color as a hex string (e.g. "#333333") or named color.
    /// </summary>
    public FenestralColor? Background { get; set; }

    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    public static TrayMenuItem Separator() => new() { IsSeparator = true };

    /// <summary>
    /// Initializes a new empty menu item.
    /// </summary>
    public TrayMenuItem()
    {
    }

    /// <summary>
    /// Initializes a new menu item with the specified text and click action.
    /// </summary>
    public TrayMenuItem(string text, Action action)
    {
        Text = text;
        Action = action;
    }
}
