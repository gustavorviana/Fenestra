namespace Fenestra.Windows.Models;

/// <summary>Represents an image in a toast notification.</summary>
public class ToastImage
{
    public string Source { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public ToastImagePlacement Placement { get; set; }
    public ToastImageCrop Crop { get; set; }
    public int? HintOverlay { get; set; }
}
