namespace Fenestra.Core.Models;

/// <summary>Defines a header for grouping toast notifications in Action Center.</summary>
public class ToastHeader
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public ToastActivationType ActivationType { get; set; }
}
