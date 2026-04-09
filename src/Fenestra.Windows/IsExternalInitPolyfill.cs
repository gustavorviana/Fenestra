// Polyfill for records/init-only properties on net472.
// The `init` accessor is compiled as a modreq on System.Runtime.CompilerServices.IsExternalInit,
// which only exists in net5.0+. This empty type fills the gap on older TFMs.

#if NETFRAMEWORK || NETSTANDARD2_0

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

#endif
