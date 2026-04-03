namespace Fenestra.Core;

public interface IFenestraComponent : IDisposable
{
    bool Disposed { get; }
}
