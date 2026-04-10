using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// A named category in the Jump List. Collects files and/or tasks that will be
/// committed to the shell on the next <see cref="IJumpListService.Apply"/> call.
/// </summary>
/// <remarks>
/// Obtained via <see cref="IJumpListService.AddCategory"/>. Calling
/// <see cref="IJumpListService.AddCategory"/> again with the same name returns the
/// same instance, so items accumulate across multiple calls.
/// </remarks>
public interface IJumpListCategory
{
    /// <summary>The display name shown in the Jump List for this category.</summary>
    string Name { get; }

    /// <summary>
    /// Adds a file entry to this category. Clicking the entry in the Jump List opens
    /// the file with its registered handler (or the "How do you want to open this?"
    /// dialog if no handler is registered). The display name is the file name.
    /// </summary>
    /// <param name="path">Absolute path to the file. Must exist at <see cref="IJumpListService.Apply"/> time.</param>
    /// <returns>This category, for fluent chaining.</returns>
    IJumpListCategory AddFile(string path);

    /// <summary>
    /// Adds a task entry to this category. Clicking it launches the configured
    /// executable with the specified arguments. <see cref="JumpListTask.ApplicationPath"/>
    /// is auto-filled with the current <c>.exe</c> when left <c>null</c>.
    /// </summary>
    /// <returns>This category, for fluent chaining.</returns>
    IJumpListCategory AddTask(JumpListTask task);

    /// <summary>
    /// Returns the files currently buffered in this category.
    /// </summary>
    IReadOnlyList<string> Files { get; }

    /// <summary>
    /// Returns the tasks currently buffered in this category.
    /// </summary>
    IReadOnlyList<JumpListTask> Tasks { get; }

    /// <summary>
    /// Removes all buffered files and tasks from this category. The category itself
    /// remains registered — call <see cref="IJumpListService.RemoveCategory"/> to
    /// remove it entirely.
    /// </summary>
    void Clear();
}
