using Fenestra.Windows.Models;

namespace Fenestra.Windows;

/// <summary>
/// Manages the Windows taskbar Jump List for the application — the menu that appears on
/// right-click of the taskbar icon. Supports multiple named categories, each containing
/// files and/or tasks, plus a built-in "user tasks" section.
/// </summary>
/// <remarks>
/// <para>
/// Framework-agnostic: lives in <c>Fenestra.Windows</c> and talks directly to the shell
/// via <c>ICustomDestinationList</c> COM.
/// </para>
/// <para>
/// All mutations are buffered. Call <see cref="Apply"/> to commit to the shell.
/// </para>
/// </remarks>
public interface IJumpListService
{
    /// <summary>
    /// Gets or creates a named category. If a category with the same name already
    /// exists, returns the existing instance — items accumulate across calls.
    /// Use the returned <see cref="IJumpListCategory"/> to add files and tasks.
    /// </summary>
    /// <param name="name">
    /// Display name shown in the Jump List (e.g. "Recent Projects", "Favorites").
    /// </param>
    IJumpListCategory AddCategory(string name);

    /// <summary>
    /// Removes a category and all its buffered items by name. No-op if the category
    /// doesn't exist. <see cref="Apply"/> must be called for the change to take effect
    /// — the category disappears from the Jump List on the next commit.
    /// </summary>
    void RemoveCategory(string name);

    /// <summary>
    /// Returns all categories currently buffered, in insertion order.
    /// </summary>
    IReadOnlyList<IJumpListCategory> Categories { get; }

    /// <summary>
    /// Replaces the built-in "Tasks" section (pinned user tasks that always appear at
    /// the bottom of the Jump List). Pass an empty array to clear.
    /// <see cref="Apply"/> must be called for the change to take effect.
    /// </summary>
    void SetTasks(params JumpListTask[] tasks);

    /// <summary>
    /// Registers a file as a recently-used document with the shell via
    /// <c>SHAddToRecentDocs</c>. This feeds the OS-level "Recent" tracking for the
    /// current AUMID — the file will appear in the shell's recent activity, file
    /// explorer recent items, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is independent of custom categories created via <see cref="AddCategory"/>.
    /// To show the file in the Jump List, you still need a category with the file
    /// added via <see cref="IJumpListCategory.AddFile"/> + <see cref="Apply"/>.
    /// This method handles the OS-level tracking only.
    /// </para>
    /// <para>
    /// The file must exist and its extension should have a registered file-type handler
    /// pointing to your app for the shell to accept it. Otherwise the call is silently
    /// ignored by the OS.
    /// </para>
    /// </remarks>
    /// <param name="path">Absolute path to the file.</param>
    void AddRecentFile(string path);

    /// <summary>
    /// Commits the buffered categories and tasks to the shell. Replaces the entire
    /// Jump List — categories not present in the current buffer will disappear.
    /// </summary>
    void Apply();

    /// <summary>
    /// Deletes the entire Jump List from the shell (all categories, tasks, and recent
    /// items). Also clears the internal buffer.
    /// </summary>
    void DeleteList();
}
