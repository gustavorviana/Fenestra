using System.Runtime.InteropServices;

namespace Fenestra.Windows.Native;

/// <summary>
/// RAII wrapper for COM RCW (Runtime Callable Wrapper) objects.
/// Ensures <c>Marshal.ReleaseComObject</c> is called on <see cref="Dispose"/>,
/// preventing COM reference leaks.
/// <para>Usage: <c>using var factory = new ComRef&lt;IFoo&gt;(rcw);</c></para>
/// <para>Null-safe: <c>using</c> on a null <c>ComRef</c> is a no-op.</para>
/// </summary>
internal sealed class ComRef<T> : IDisposable where T : class
{
    public readonly T Value;

    public ComRef(T value) => Value = value;

    public void Dispose() => Marshal.ReleaseComObject(Value);

    public static implicit operator T(ComRef<T> r) => r.Value;
}
