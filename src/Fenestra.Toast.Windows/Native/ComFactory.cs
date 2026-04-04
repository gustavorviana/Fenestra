using System.Collections.Concurrent;

namespace Fenestra.Toast.Windows.Native;

internal static class ComFactory
{
    private static readonly ConcurrentDictionary<Guid, Type> _cache = new();

    /// <summary>
    /// Creates a COM instance from a CLSID and casts it to the desired interface.
    /// </summary>
    public static T Create<T>(Guid clsid)
    {
        var type = _cache.GetOrAdd(clsid, static id => Type.GetTypeFromCLSID(id)!);

        var instance = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create COM instance for CLSID '{clsid}'.");

        return (T)instance;
    }
}