namespace Fenestra.Core.Models;

/// <summary>Represents a progress bar in a toast notification.</summary>
public class ToastProgress
{
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? ValueOverride { get; set; }
}
