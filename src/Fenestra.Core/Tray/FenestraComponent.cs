namespace Fenestra.Core.Tray;

public abstract class FenestraComponent : IDisposable
{
    private bool _disposed;

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