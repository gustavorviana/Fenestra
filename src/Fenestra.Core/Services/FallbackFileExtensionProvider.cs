using Fenestra.Core.Models;

namespace Fenestra.Core.Services;

/// <summary>
/// Managed, OS-agnostic <see cref="IFileExtensionProvider"/>. Resolves a small
/// table of common extensions and falls back to a generic <c>"{EXT} File"</c>
/// label for everything else.
///
/// <para>
/// This is the default provider registered by the core builder, and also serves
/// as the graceful-degradation path for native providers when the platform API
/// is unavailable or returns nothing.
/// </para>
/// </summary>
public sealed class FallbackFileExtensionProvider : IFileExtensionProvider
{
    private static readonly Dictionary<string, string> _known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["txt"] = "Text Document",
        ["log"] = "Text Document",
        ["csv"] = "CSV File",
        ["json"] = "JSON File",
        ["xml"] = "XML Document",
        ["ini"] = "Configuration Settings",
        ["cfg"] = "Configuration Settings",
        ["pdf"] = "PDF Document",
        ["doc"] = "Word Document",
        ["docx"] = "Word Document",
        ["xls"] = "Excel Worksheet",
        ["xlsx"] = "Excel Worksheet",
        ["ppt"] = "PowerPoint Presentation",
        ["pptx"] = "PowerPoint Presentation",
        ["rtf"] = "Rich Text Document",
        ["png"] = "PNG Image",
        ["jpg"] = "JPEG Image",
        ["jpeg"] = "JPEG Image",
        ["gif"] = "GIF Image",
        ["bmp"] = "Bitmap Image",
        ["ico"] = "Icon",
        ["svg"] = "SVG Image",
        ["zip"] = "ZIP Archive",
        ["rar"] = "RAR Archive",
        ["7z"] = "7-Zip Archive",
        ["gz"] = "GZip Archive",
        ["tar"] = "TAR Archive",
        ["exe"] = "Application",
        ["dll"] = "Application Extension",
        ["bat"] = "Windows Batch File",
        ["ps1"] = "PowerShell Script",
        ["sh"] = "Shell Script",
        ["mp3"] = "MP3 Audio",
        ["wav"] = "WAV Audio",
        ["mp4"] = "MP4 Video",
        ["mkv"] = "Matroska Video",
        ["html"] = "HTML Document",
        ["htm"] = "HTML Document",
    };

    /// <inheritdoc />
    public string GetDescription(string extension)
    {
        var token = FileExtensionInfo.NormalizeExtension(extension);
        if (token == "*")
            return "All Files";

        return _known.TryGetValue(token, out var description)
            ? description
            : $"{token.ToUpperInvariant()} File";
    }

    /// <inheritdoc />
    public FileExtensionInfo GetInfo(string extension)
    {
        var token = FileExtensionInfo.NormalizeExtension(extension);
        return new FileExtensionInfo(token, GetDescription(extension));
    }
}
