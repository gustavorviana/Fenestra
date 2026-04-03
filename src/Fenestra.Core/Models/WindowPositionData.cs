namespace Fenestra.Core.Models;

/// <summary>
/// Stores the saved position, size, and state of a window.
/// </summary>
public class WindowPositionData
{
    /// <summary>
    /// Gets or sets the left edge position of the window.
    /// </summary>
    public double Left { get; set; }

    /// <summary>
    /// Gets or sets the top edge position of the window.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// Gets or sets the width of the window.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the window.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the window state (normal, minimized, or maximized).
    /// </summary>
    public int State { get; set; }
}
