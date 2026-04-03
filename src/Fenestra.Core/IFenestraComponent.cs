namespace Fenestra.Core;

/// <summary>
/// Base interface for disposable Fenestra components.
/// </summary>
public interface IFenestraComponent : IDisposable
{
    /// <summary>
    /// Gets whether this component has been disposed.
    /// </summary>
    bool Disposed { get; }
}
