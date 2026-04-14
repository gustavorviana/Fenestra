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

    /// <summary>
    /// Queues an action for execution on the main thread and returns immediately
    /// without waiting for it to complete. Exceptions raised by the action are not
    /// observed by the caller.
    /// </summary>
    void BeginInvoke(Action action);
}
