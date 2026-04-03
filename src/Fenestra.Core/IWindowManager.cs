namespace Fenestra.Core;

/// <summary>
/// Manages window lifecycle including creation, tracking, and closing via dependency injection.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Creates and shows a window of the specified type.
    /// </summary>
    T Show<T>() where T : class;

    /// <summary>
    /// Creates and shows a window of the specified type with the given data context.
    /// </summary>
    T Show<T>(object dataContext) where T : class;

    /// <summary>
    /// Shows a modal dialog and returns true if the result is affirmative.
    /// </summary>
    bool ShowDialog<T>() where T : class, IDialog;

    /// <summary>
    /// Shows a modal dialog with the given data context and returns true if the result is affirmative.
    /// </summary>
    bool ShowDialog<T>(object dataContext) where T : class, IDialog;

    /// <summary>
    /// Shows a modal dialog and returns a typed result.
    /// </summary>
    TResult ShowDialog<T, TResult>() where T : class, IDialog<TResult>;

    /// <summary>
    /// Shows a modal dialog with the given data context and returns a typed result.
    /// </summary>
    TResult ShowDialog<T, TResult>(object dataContext) where T : class, IDialog<TResult>;

    /// <summary>
    /// Returns an existing open window of the specified type, or null if none is open.
    /// </summary>
    T? GetOpenWindow<T>() where T : class;

    /// <summary>
    /// Closes the open window of the specified type, if one exists.
    /// </summary>
    void Close<T>() where T : class;

    /// <summary>
    /// Closes all currently open dialog windows.
    /// </summary>
    void CloseAllDialogs();
}
