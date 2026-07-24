using System.Collections.Concurrent;
using Fenestra.Core;
using Fenestra.Core.Models;
using Fenestra.Core.Services;
using Fenestra.Windows.Native;

namespace Fenestra.Windows.Services;

/// <summary>
/// Windows <see cref="IFileExtensionProvider"/> backed by the shell
/// (<c>SHGetFileInfo</c>). Resolved descriptions are cached per extension.
/// When the shell returns nothing — or the process is not on Windows — it
/// degrades to the managed <see cref="FallbackFileExtensionProvider"/>.
/// </summary>
public sealed class WindowsFileExtensionProvider : IFileExtensionProvider
{
    private readonly FallbackFileExtensionProvider _fallback = new();
    private readonly ConcurrentDictionary<string, string> _cache = new();

    /// <inheritdoc />
    public string GetDescription(string extension)
    {
         var token = FileExtensionInfo.NormalizeExtension(extension);
        if (token == "*")
            return _fallback.GetDescription(extension);

        return _cache.GetOrAdd(extension, Resolve);
    }

    /// <inheritdoc />
    public FileExtensionInfo GetInfo(string extension)
    {
        var token = FileExtensionInfo.NormalizeExtension(extension);
        return new FileExtensionInfo(token, GetDescription(extension));
    }

    private string Resolve(string token)
    {
        string typeName = string.Empty;

        if (Platform.IsWindows)
        {
            try { typeName = ShellFileInfoNative.GetTypeName("." + token); }
            catch { typeName = string.Empty; }
        }

        return string.IsNullOrWhiteSpace(typeName)
            ? _fallback.GetDescription(token)
            : typeName;
    }
}
