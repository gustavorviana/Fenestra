namespace Fenestra.Core;

/// <summary>
/// Base class for disposable Fenestra components with standard dispose pattern.
/// </summary>
public abstract class FenestraComponent : IFenestraComponent
{
    private bool _disposed;

    /// <inheritdoc />
    public bool Disposed => _disposed;

    /// <summary>
    /// Releases resources; override in subclasses to add custom cleanup logic.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
        }

        _disposed = true;

    }

    ~FenestraComponent()
    {
        Dispose(disposing: false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}