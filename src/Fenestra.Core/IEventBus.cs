namespace Fenestra.Core;

public delegate Task EventHandler<in T>(T message);

public interface IEventBus
{
    Task PublishAsync<T>(T message) where T : notnull;
    IDisposable On<T>(EventHandler<T> handler) where T : notnull;
}
