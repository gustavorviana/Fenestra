using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Fenestra.Toast.Windows.Native;

internal static class ComFactory
{
    private static readonly ConcurrentDictionary<Guid, Type> _typeCache = new();
    private static readonly ConcurrentDictionary<(IntPtr, Type), Delegate> _delegateCache = new();

    /// <summary>
    /// Creates a COM instance from a CLSID and casts it to the desired interface.
    /// </summary>
    public static T Create<T>(Guid clsid)
    {
        var type = _typeCache.GetOrAdd(clsid, static id => Type.GetTypeFromCLSID(id)!);

        var instance = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create COM instance for CLSID '{clsid:B}'.");

        return (T)instance;
    }

    /// <summary>
    /// Gets a cached delegate for a native function pointer. Avoids repeated marshalling overhead.
    /// The cache is keyed by (function pointer, delegate type) to handle the same pointer used with different signatures.
    /// </summary>
    public static T GetDelegate<T>(IntPtr functionPointer) where T : Delegate
    {
        if (functionPointer == IntPtr.Zero)
            throw new ArgumentException("Function pointer is null.", nameof(functionPointer));

        var key = (functionPointer, typeof(T));
        return (T)_delegateCache.GetOrAdd(key,
            k => Marshal.GetDelegateForFunctionPointer<T>(k.Item1));
    }
}