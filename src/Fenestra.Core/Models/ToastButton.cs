namespace Fenestra.Core.Models;

/// <summary>Represents an action button or context menu item in a toast notification.</summary>
public class ToastButton
{
    public string? Text { get; set; }
    public string? Argument { get; set; }
    public ToastActivationType ActivationType { get; set; }
    public string? ImageUri { get; set; }
    public ToastButtonStyle Style { get; set; }
    public string? Tooltip { get; set; }
    public bool IsContextMenu { get; set; }
    public string? InputId { get; set; }
    public bool IsSnooze { get; set; }
    public bool IsDismiss { get; set; }
}
