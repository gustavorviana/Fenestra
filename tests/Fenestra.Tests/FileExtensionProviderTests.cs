using Fenestra.Core.Models;
using Fenestra.Core.Services;
using Xunit;

namespace Fenestra.Tests;

public class FileExtensionNormalizeTests
{
    [Theory]
    [InlineData("txt", "txt")]
    [InlineData(".txt", "txt")]
    [InlineData("*.txt", "txt")]
    [InlineData("*.PNG", "png")]
    [InlineData(".Json", "json")]
    [InlineData("*", "*")]
    [InlineData("*.*", "*")]
    [InlineData(".*", "*")]
    [InlineData("", "*")]
    [InlineData("   ", "*")]
    public void Normalizes_to_bare_lowercase_token(string input, string expected)
    {
        Assert.Equal(expected, FileExtensionInfo.NormalizeExtension(input));
    }
}

public class FallbackFileExtensionProviderTests
{
    private readonly FallbackFileExtensionProvider _sut = new();

    [Fact]
    public void Known_extension_returns_mapped_description()
    {
        Assert.Equal("Text Document", _sut.GetDescription(".txt"));
    }

    [Fact]
    public void Unknown_extension_returns_generic_label()
    {
        Assert.Equal("FOOBAR File", _sut.GetDescription(".foobar"));
    }

    [Fact]
    public void Wildcard_returns_all_files()
    {
        Assert.Equal("All Files", _sut.GetDescription("*"));
    }

    [Fact]
    public void GetInfo_normalizes_extension_and_fills_description()
    {
        var info = _sut.GetInfo("*.JSON");

        Assert.Equal("json", info.Extension);
        Assert.Equal("JSON File", info.Description);
    }

    [Fact]
    public void GetInfo_for_wildcard_matches_the_All_filter()
    {
        var info = _sut.GetInfo("*.*");

        Assert.Equal("*", info.Extension);
        Assert.Equal("All Files", info.Description);
    }
}
