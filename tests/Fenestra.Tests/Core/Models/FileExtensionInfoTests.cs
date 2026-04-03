using Fenestra.Core.Models;

namespace Fenestra.Tests.Core.Models;

public class FileExtensionInfoTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var info = new FileExtensionInfo("txt", "Text Files");

        Assert.Equal("txt", info.Extension);
        Assert.Equal("Text Files", info.Description);
    }

    [Fact]
    public void All_ReturnsWildcard()
    {
        var all = FileExtensionInfo.All;

        Assert.Equal("*", all.Extension);
        Assert.Equal("All Files", all.Description);
    }
}
