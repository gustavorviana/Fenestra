namespace Fenestra.Windows.Models;

/// <summary>Represents an adaptive group containing subgroups for multi-column toast layouts.</summary>
public class ToastGroup
{
    public List<ToastSubgroup> Subgroups { get; } = new();
}
