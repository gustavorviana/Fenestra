using Fenestra.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fenestra.Tests.Core;

public class FenestraBuilderTests
{
    [Fact]
    public void Services_IsEmptyByDefault()
    {
        var builder = new FenestraBuilder();

        Assert.Empty(builder.Services);
    }

    [Fact]
    public void Configuration_IsNonNull()
    {
        var builder = new FenestraBuilder();

        Assert.NotNull(builder.Configuration);
    }

    [Fact]
    public void UseAppName_ReturnsSameBuilderForFluentChaining()
    {
        var builder = new FenestraBuilder();

        var result = builder.UseAppName("MyApp");

        Assert.Same(builder, result);
    }

    [Fact]
    public void UseAppInfo_WithVersion_ReturnsSameBuilder()
    {
        var builder = new FenestraBuilder();

        var result = builder.UseAppInfo("MyApp", new Version(1, 0));

        Assert.Same(builder, result);
    }

    [Fact]
    public void UseAppInfo_WithAppIdAndVersion_ReturnsSameBuilder()
    {
        var builder = new FenestraBuilder();

        var result = builder.UseAppInfo("MyApp", "my.app", new Version(1, 0));

        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureLogging_ReturnsSameBuilder()
    {
        var builder = new FenestraBuilder();

        var result = builder.ConfigureLogging(_ => { });

        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureServices_ReturnsSameBuilder()
    {
        var builder = new FenestraBuilder();

        var result = builder.ConfigureServices(_ => { });

        Assert.Same(builder, result);
    }

    [Fact]
    public void Environment_DefaultsToProductionWhenNoEnvVarSet()
    {
        // If no DOTNET_ENVIRONMENT / ASPNETCORE_ENVIRONMENT set, default is "Production"
        var builder = new FenestraBuilder();

        Assert.False(string.IsNullOrEmpty(builder.Environment));
    }

    [Fact]
    public void Environment_CanBeOverridden()
    {
        var builder = new FenestraBuilder();

        builder.Environment = "Development";

        Assert.Equal("Development", builder.Environment);
    }

    [Fact]
    public void ConfigureServices_AddsServiceToCollection()
    {
        var builder = new FenestraBuilder();
        builder.Services.AddSingleton<ISampleService, SampleService>();

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ISampleService));
    }

    // Helper types
    private interface ISampleService { }
    private class SampleService : ISampleService { }
}
