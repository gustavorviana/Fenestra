namespace Fenestra.Core;

/// <summary>
/// Async event handler delegate for the event bus.
/// </summary>
public delegate Task BusHandler<in T>(T message);

/// <summary>
/// Typed async pub/sub event bus. Publish events from one point and receive them in another without direct coupling.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a message to all subscribers of the specified type.
    /// </summary>
    Task PublishAsync<T>(T message) where T : notnull;

    /// <summary>
    /// Subscribes a handler for messages of the specified type; dispose the return value to unsubscribe.
    /// </summary>
    IDisposable On<T>(BusHandler<T> handler) where T : notnull;
}
