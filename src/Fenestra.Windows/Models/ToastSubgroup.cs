namespace Fenestra.Windows.Models;

/// <summary>Represents a subgroup (column) in an adaptive toast layout.</summary>
public class ToastSubgroup
{
    public int? Weight { get; set; }
    public ToastTextStacking TextStacking { get; set; }
    public List<ToastText> Texts { get; } = new();
    public List<ToastImage> Images { get; } = new();
}
