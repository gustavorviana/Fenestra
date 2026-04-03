namespace Fenestra.Core.Exceptions;

#if NETFRAMEWORK
[Serializable]
#endif
public class LaunchWindowException : Exception
{
    public Type WindowType { get; }

    public LaunchWindowException(Type windowType)
        : base($"Failed to launch window of type '{windowType.FullName}'.")
    {
        WindowType = windowType;
    }

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
