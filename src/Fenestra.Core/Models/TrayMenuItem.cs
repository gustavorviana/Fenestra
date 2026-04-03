using Fenestra.Core.Drawing;

namespace Fenestra.Core.Models;

public class TrayMenuItem
{
    public string? Text { get; set; }
    public Action? Action { get; set; }
    public bool IsSeparator { get; set; }
    public bool IsEnabled { get; set; } = true;
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

    public static TrayMenuItem Separator() => new() { IsSeparator = true };

    public TrayMenuItem()
    {
    }

    public TrayMenuItem(string text, Action action)
    {
        Text = text;
        Action = action;
    }
}
