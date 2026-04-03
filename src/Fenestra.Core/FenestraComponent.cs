namespace Fenestra.Core;

public abstract class FenestraComponent : IFenestraComponent
{
    private bool _disposed;

    public bool Disposed => _disposed;

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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}