using Fenestra.Windows.Services;

namespace Fenestra.Windows.Tests.Services;

/// <summary>
/// Exercises the shell-backed provider against the real Windows shell. Assertions
/// stay resilient to locale/shell-version differences by checking for a non-empty,
/// non-generic description rather than exact strings.
/// </summary>
public sealed class WindowsFileExtensionProviderTests
{
    private readonly WindowsFileExtensionProvider _sut = new();

    [Fact]
    public void Resolves_a_non_empty_description_for_known_extension()
    {
        var description = _sut.GetDescription(".txt");

        Assert.False(string.IsNullOrWhiteSpace(description));
    }

    [Fact]
    public void GetInfo_normalizes_the_extension_token()
    {
        var info = _sut.GetInfo("*.TXT");

        Assert.Equal("txt", info.Extension);
        Assert.False(string.IsNullOrWhiteSpace(info.Description));
    }

    [Fact]
    public void Wildcard_falls_back_to_all_files()
    {
        Assert.Equal("All Files", _sut.GetDescription("*.*"));
    }

    [Fact]
    public void Unknown_extension_still_yields_a_description_via_fallback()
    {
        // A made-up extension the shell has no registration for must not come
        // back empty — the managed fallback fills it in.
        var description = _sut.GetDescription(".fenestra_nope_xyz");

        Assert.False(string.IsNullOrWhiteSpace(description));
    }

    [Fact]
    public void Repeated_calls_are_consistent_cached()
    {
        var first = _sut.GetDescription(".txt");
        var second = _sut.GetDescription(".txt");

        Assert.Equal(first, second);
    }
}
