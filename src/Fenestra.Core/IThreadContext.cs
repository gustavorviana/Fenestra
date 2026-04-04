namespace Fenestra.Core;

/// <summary>
/// Abstracts thread dispatching for invoking actions on the application's main thread.
/// </summary>
public interface IThreadContext
{
    /// <summary>
    /// Invokes an action synchronously on the main thread.
    /// </summary>
    void Invoke(Action action);

    /// <summary>
    /// Invokes an action asynchronously on the main thread.
    /// </summary>
    Task InvokeAsync(Action action);
}
