namespace Fenestra.Core;

/// <summary>
/// Default <see cref="IThreadContext"/> implementation using <see cref="SynchronizationContext"/>.
/// Falls back to direct invocation if no context is available.
/// </summary>
public class SyncContextThreadContext : IThreadContext
{
    private SynchronizationContext? _context;

    /// <summary>
    /// Captures the current <see cref="SynchronizationContext"/> automatically.
    /// </summary>
    public SyncContextThreadContext()
    {
        _context = SynchronizationContext.Current;
    }

    /// <summary>
    /// Uses the specified <see cref="SynchronizationContext"/>.
    /// </summary>
    public SyncContextThreadContext(SynchronizationContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sets or replaces the synchronization context.
    /// </summary>
    public void SetContext(SynchronizationContext? context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void Invoke(Action action)
    {
        if (_context == null)
        {
            action();
            return;
        }

        var done = new ManualResetEventSlim(false);
        Exception? caught = null;

        _context.Post(_ =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
            finally { done.Set(); }
        }, null);

        done.Wait();
        if (caught != null) throw caught;
    }

    /// <inheritdoc />
    public Task InvokeAsync(Action action)
    {
        if (_context == null)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();

        _context.Post(_ =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);

        return tcs.Task;
    }
}
