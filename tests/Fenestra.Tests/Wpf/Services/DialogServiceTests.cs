using Fenestra.Core.Models;
using Fenestra.Wpf.Services;

namespace Fenestra.Tests.Wpf.Services;

public class DialogServiceTests
{
    [Fact]
    public void BuildFilterString_SingleExtension()
    {
        var extensions = new[] { new FileExtensionInfo("txt", "Text Files") };

        var result = DialogService.BuildFilterString(extensions);

        Assert.Equal("Text Files|*.txt", result);
    }

    [Fact]
    public void BuildFilterString_MultipleExtensions()
    {
        var extensions = new[]
        {
            new FileExtensionInfo("txt", "Text Files"),
            new FileExtensionInfo("csv", "CSV Files")
        };

        var result = DialogService.BuildFilterString(extensions);

        Assert.Equal("Text Files|*.txt|CSV Files|*.csv", result);
    }

    [Fact]
    public void BuildFilterString_AllFiles()
    {
        var extensions = new[] { FileExtensionInfo.All };

        var result = DialogService.BuildFilterString(extensions);

        Assert.Equal("All Files|*.*", result);
    }
}
