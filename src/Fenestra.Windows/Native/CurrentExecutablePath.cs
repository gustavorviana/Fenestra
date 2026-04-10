using System.Diagnostics;
using System.Reflection;

namespace Fenestra.Windows.Native;

/// <summary>
/// Lazy cache for the current executable's <c>.exe</c> path. Used by features that
/// need to point the shell at the host binary (Jump List tasks, Start Menu shortcuts,
/// registered COM servers) — the correct value is <see cref="Process.MainModule"/>'s
/// <c>FileName</c>, not <see cref="Assembly.GetEntryAssembly"/>'s <c>Location</c>,
/// which on .NET 8 points at the entry-assembly <c>.dll</c> rather than the host exe.
/// </summary>
internal static class CurrentExecutablePath
{
    private static string? _cached;

    public static string Get()
    {
        if (_cached is not null) return _cached;

        try
        {
            using var process = Process.GetCurrentProcess();
            var path = process.MainModule?.FileName;
            if (!string.IsNullOrEmpty(path))
                return _cached = System.IO.Path.GetFullPath(path);
        }
        catch
        {
            // MainModule may throw under restricted integrity levels — fall through.
        }

        return _cached = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
    }
}
