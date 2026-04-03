namespace Fenestra.Core;

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public async Task PublishAsync<T>(T message) where T : notnull
    {
        Delegate[] snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            snapshot = list.ToArray();
        }

        foreach (var handler in snapshot)
            await ((EventHandler<T>)handler)(message);
    }

    public IDisposable On<T>(EventHandler<T> handler) where T : notnull
    {
        lock (_lock)
        {
            if (!_handlers.ContainsKey(typeof(T)))
                _handlers[typeof(T)] = new();

            _handlers[typeof(T)].Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                        _handlers.Remove(typeof(T));
                }
            }
        });
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _unsubscribe;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
