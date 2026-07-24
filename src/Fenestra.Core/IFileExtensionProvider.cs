using Fenestra.Core.Models;

namespace Fenestra.Core;

/// <summary>
/// Resolves human-readable metadata for file extensions (e.g. the OS type
/// description of <c>.txt</c> -&gt; "Text Document").
///
/// <para>
/// This is an OS-neutral abstraction: each platform library (Windows, and macOS
/// / Linux in the future) registers its own native provider through its builder.
/// When no native provider is available, a managed fallback is used, so the
/// contract works on every OS in a degraded-but-functional form.
/// </para>
/// </summary>
public interface IFileExtensionProvider
{
    /// <summary>
    /// Returns the human-readable type description for an extension
    /// (e.g. <c>.txt</c> -&gt; "Text Document"), or a generic label when the
    /// type is unknown. Accepts <c>"txt"</c>, <c>".txt"</c>, <c>"*.txt"</c>, or
    /// <c>"*"</c> (all files).
    /// </summary>
    string GetDescription(string extension);

    /// <summary>
    /// Returns a populated <see cref="FileExtensionInfo"/> — the normalized
    /// extension token plus its resolved description — ready for use in a file
    /// dialog filter.
    /// </summary>
    FileExtensionInfo GetInfo(string extension);
}
