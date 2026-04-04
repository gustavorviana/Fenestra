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
    public string? InputId { get; set; }
    public ToastButtonType Type { get; set; }
}

/// <summary>Specifies the behavior type of a toast button.</summary>
public enum ToastButtonType
{
    /// <summary>Standard action button.</summary>
    Action,

    /// <summary>Context menu item (shown on right-click).</summary>
    ContextMenu,

    /// <summary>System-managed snooze button.</summary>
    Snooze,

    /// <summary>System-managed dismiss button.</summary>
    Dismiss
}
