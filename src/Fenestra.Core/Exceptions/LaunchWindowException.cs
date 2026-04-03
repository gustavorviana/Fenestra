namespace Fenestra.Core.Exceptions;

#if NETFRAMEWORK
[Serializable]
#endif
/// <summary>
/// Thrown when a window cannot be resolved from the dependency injection container.
/// </summary>
public class LaunchWindowException : Exception
{
    /// <summary>
    /// Gets the type of window that failed to launch.
    /// </summary>
    public Type WindowType { get; }

    /// <summary>
    /// Initializes a new instance for the specified window type.
    /// </summary>
    public LaunchWindowException(Type windowType)
        : base($"Failed to launch window of type '{windowType.FullName}'.")
    {
        WindowType = windowType;
    }

    /// <summary>
    /// Initializes a new instance for the specified window type with an inner exception.
    /// </summary>
    public LaunchWindowException(Type windowType, Exception innerException)
        : base($"Failed to launch window of type '{windowType.FullName}'.", innerException)
    {
        WindowType = windowType;
    }

#if NETFRAMEWORK
    protected LaunchWindowException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
        WindowType = typeof(object);
    }
#endif
}
