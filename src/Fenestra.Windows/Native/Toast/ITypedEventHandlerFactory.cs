namespace Fenestra.Windows.Native.Toast;

/// <summary>
/// Abstraction over <see cref="TypedEventHandlerFactory"/> static API to enable mocking
/// of COM event handler creation/release in tests.
/// </summary>
internal interface ITypedEventHandlerFactory
{
    /// <summary>
    /// Creates a COM event handler that calls <paramref name="callback"/> when Invoke is called.
    /// The returned IntPtr is a COM object with refCount=1. The caller owns this reference.
    /// </summary>
    IntPtr Create(Guid iid, Action<IntPtr, IntPtr> callback);

    /// <summary>
    /// Releases the caller's reference to a handler created by <see cref="Create"/>.
    /// </summary>
    void Release(IntPtr handler);
}

/// <summary>
/// Default implementation that delegates to the static <see cref="TypedEventHandlerFactory"/>.
/// </summary>
internal sealed class DefaultTypedEventHandlerFactory : ITypedEventHandlerFactory
{
    public static readonly DefaultTypedEventHandlerFactory Instance = new();

    public IntPtr Create(Guid iid, Action<IntPtr, IntPtr> callback)
        => TypedEventHandlerFactory.Create(iid, callback);

    public void Release(IntPtr handler)
        => TypedEventHandlerFactory.Release(handler);
}
