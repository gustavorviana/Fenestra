namespace Fenestra.Windows.Models;

/// <summary>
/// Framework-agnostic description of a custom Jump List task. Mirrors the subset of
/// <c>System.Windows.Shell.JumpTask</c> that matters, without the WPF dependency.
/// </summary>
public sealed class JumpListTask
{
    /// <summary>Title displayed in the Jump List (required).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional tooltip / description shown on hover.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Executable to launch when the task is clicked. Leave <c>null</c> to let
    /// <c>JumpListService</c> auto-fill it with the current process's <c>.exe</c> path
    /// — matching the Start Menu shortcut's target, which is required for Windows to
    /// accept the task.
    /// </summary>
    public string? ApplicationPath { get; set; }

    /// <summary>Optional command-line arguments forwarded on launch.</summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Working directory set on launch. Defaults to the executable's directory when
    /// <c>null</c>.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Path to a resource (<c>.ico</c>, <c>.exe</c>, or <c>.dll</c>) containing the
    /// task's icon. Leave <c>null</c> to use the executable's default icon.
    /// </summary>
    public string? IconResourcePath { get; set; }

    /// <summary>
    /// Index of the icon inside <see cref="IconResourcePath"/>. Ignored when the path
    /// is a standalone <c>.ico</c> file.
    /// </summary>
    public int IconResourceIndex { get; set; }
}
