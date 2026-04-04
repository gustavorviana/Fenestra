namespace Fenestra.Core.Models;

/// <summary>Represents a text element in adaptive toast content with styling attributes.</summary>
public class ToastText
{
    public string Text { get; set; } = string.Empty;
    public ToastTextStyle Style { get; set; }
    public bool Wrap { get; set; }
    public int? MaxLines { get; set; }
    public int? MinLines { get; set; }
    public ToastTextAlign Align { get; set; }
}
