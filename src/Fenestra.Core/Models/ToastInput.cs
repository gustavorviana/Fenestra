namespace Fenestra.Core.Models;

/// <summary>Represents a text or selection input in a toast notification.</summary>
public class ToastInput
{
    public string Id { get; set; } = string.Empty;
    public ToastInputType Type { get; set; }
    public string? Title { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public Dictionary<string, string> Selections { get; } = new();
}

/// <summary>Specifies the type of a toast input control.</summary>
public enum ToastInputType { Text, Selection }
