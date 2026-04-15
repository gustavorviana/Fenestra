using Fenestra.Core.Models;
using Fenestra.Windows.Localization;
using System.Resources;

namespace Fenestra.Windows.Tests.Localization;

/// <summary>
/// Unit tests for <see cref="LocalizationRegistryBuilder"/>. Covers the pure filtering /
/// alias-building logic (<see cref="LocalizationRegistryBuilder.EnumerateAutoDiscovered"/>)
/// and the duplicate-resolution behavior
/// (<see cref="LocalizationRegistryBuilder.Register"/>).
///
/// <para>
/// Tests that exercise <c>Register</c> mutate the process-wide
/// <see cref="TranslationSource.Instance"/> singleton, so each test clears it at
/// construction and dispose time to avoid leaking state between tests.
/// </para>
/// </summary>
public class LocalizationRegistryBuilderTests : IDisposable
{
    public LocalizationRegistryBuilderTests()
    {
        TranslationSource.Instance.ClearResourceManagers();
    }

    public void Dispose()
    {
        TranslationSource.Instance.ClearResourceManagers();
    }

    // =====================================================================
    // EnumerateAutoDiscovered — filtering
    // =====================================================================

    [Fact]
    public void EnumerateAutoDiscovered_SkipsNonResourceFiles()
    {
        var names = new[]
        {
            "MyApp.Resources.Messages.resources",
            "MyApp.SomeImage.png",
            "MyApp.SomeXml.xml",
        };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null)
            .ToArray();

        Assert.Single(result);
        Assert.Equal(("MyApp.Resources.Messages", "messages"), result[0]);
    }

    [Fact]
    public void EnumerateAutoDiscovered_SkipsGeneratedXamlBundle()
    {
        // WPF always embeds {AssemblyName}.g.resources — it's BAML, not a string bundle.
        var names = new[]
        {
            "MyApp.Resources.Messages.resources",
            "MyApp.g.resources",
        };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null)
            .Select(x => x.Alias)
            .ToArray();

        Assert.Equal(new[] { "messages" }, result);
    }

    [Fact]
    public void EnumerateAutoDiscovered_StripsResourcesSuffixFromEndOnly()
    {
        // Pathological but valid: a folder literally named "resources" somewhere in the path.
        // The previous implementation used Replace(".resources", "") which would strip the
        // middle occurrence too. The anchored trim keeps the middle intact.
        var names = new[] { "MyApp.resources.Messages.resources" };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null)
            .ToArray();

        Assert.Single(result);
        Assert.Equal("MyApp.resources.Messages", result[0].BaseName);
        Assert.Equal("messages", result[0].Alias);
    }

    [Fact]
    public void EnumerateAutoDiscovered_AppliesPrefix()
    {
        var names = new[]
        {
            "MyApp.Resources.Messages.resources",
            "MyApp.Resources.Errors.resources",
        };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: "app")
            .Select(x => x.Alias)
            .ToArray();

        Assert.Equal(new[] { "app.messages", "app.errors" }, result);
    }

    [Fact]
    public void EnumerateAutoDiscovered_LowercasesPrefixAndShortName()
    {
        var names = new[] { "MyApp.Resources.Messages.resources" };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: "APP")
            .Single();

        Assert.Equal("app.messages", result.Alias);
    }

    [Fact]
    public void EnumerateAutoDiscovered_NamespaceFilter_IncludesOnlyMatchingBundles()
    {
        var names = new[]
        {
            "MyApp.Resources.Messages.resources",
            "MyApp.Other.Thing.resources",
            "ThirdParty.Strings.Foo.resources",
        };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null, namespaceFilter: "MyApp.Resources")
            .Select(x => x.BaseName)
            .ToArray();

        Assert.Equal(new[] { "MyApp.Resources.Messages" }, result);
    }

    [Fact]
    public void EnumerateAutoDiscovered_NamespaceFilter_IsCaseInsensitive()
    {
        var names = new[] { "MyApp.Resources.Messages.resources" };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null, namespaceFilter: "myapp.resources")
            .ToArray();

        Assert.Single(result);
    }

    [Fact]
    public void EnumerateAutoDiscovered_EmptyOrNullInput_ReturnsEmpty()
    {
        Assert.Empty(LocalizationRegistryBuilder.EnumerateAutoDiscovered(Array.Empty<string>(), null));
        Assert.Empty(LocalizationRegistryBuilder.EnumerateAutoDiscovered(null!, null));
    }

    [Fact]
    public void EnumerateAutoDiscovered_DegenerateNames_AreSkipped()
    {
        var names = new[]
        {
            "",                    // empty
            ".resources",          // empty base name
            "justaname.resources", // single-segment: base name == short name, still valid
        };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null)
            .ToArray();

        Assert.Single(result);
        Assert.Equal(("justaname", "justaname"), result[0]);
    }

    [Fact]
    public void EnumerateAutoDiscovered_NoPrefix_ShortNameIsLowercased()
    {
        var names = new[] { "MyApp.Resources.Messages.resources" };

        var result = LocalizationRegistryBuilder
            .EnumerateAutoDiscovered(names, prefix: null)
            .Single();

        Assert.Equal("messages", result.Alias);
    }

    // =====================================================================
    // Register — duplicate-resolution behaviors
    // =====================================================================

    private static ResourceManager NewManager(string baseName = "MyApp.Resources.Messages")
    {
        // ResourceManager does not need the bundle to actually exist at construction;
        // we only need two distinct instances to verify identity in the tests.
        return new ResourceManager(baseName, typeof(LocalizationRegistryBuilderTests).Assembly);
    }

    [Fact]
    public void Register_NoCollision_RegistersUnderAlias()
    {
        var manager = NewManager();

        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance,
            alias: "messages",
            manager: manager,
            baseName: "MyApp.Resources.Messages",
            behavior: DuplicateResourceBehavior.Throw);

        Assert.True(TranslationSource.Instance.HasResourceManager("messages"));
    }

    [Fact]
    public void Register_Throw_ThrowsOnCollision()
    {
        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance,
            alias: "messages",
            manager: NewManager("MyApp.Resources.Messages"),
            baseName: "MyApp.Resources.Messages",
            behavior: DuplicateResourceBehavior.Throw);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LocalizationRegistryBuilder.Register(
                TranslationSource.Instance,
                alias: "messages",
                manager: NewManager("Other.Resources.Messages"),
                baseName: "Other.Resources.Messages",
                behavior: DuplicateResourceBehavior.Throw));

        Assert.Contains("'messages'", ex.Message);
        Assert.Contains("Other.Resources.Messages", ex.Message);
    }

    [Fact]
    public void Register_Replace_OverwritesExistingAlias()
    {
        var first = NewManager("MyApp.Resources.Messages");
        var second = NewManager("Other.Resources.Messages");

        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance, "messages", first,
            "MyApp.Resources.Messages", DuplicateResourceBehavior.Throw);

        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance, "messages", second,
            "Other.Resources.Messages", DuplicateResourceBehavior.Replace);

        // Only one alias exists and it maps to `second`. We can't observe the mapping
        // directly (TranslationSource doesn't expose the manager), so we assert that
        // HasResourceManager is still true for the alias and that the full-name aliases
        // were NOT added (which would indicate the UseFullName path fired by mistake).
        Assert.True(TranslationSource.Instance.HasResourceManager("messages"));
        Assert.False(TranslationSource.Instance.HasResourceManager("other.resources.messages"));
    }

    [Fact]
    public void Register_UseFullName_RegistersSecondUnderFullName()
    {
        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance, "messages",
            NewManager("MyApp.Resources.Messages"),
            "MyApp.Resources.Messages",
            DuplicateResourceBehavior.UseFullName);

        LocalizationRegistryBuilder.Register(
            TranslationSource.Instance, "messages",
            NewManager("Other.Resources.Messages"),
            "Other.Resources.Messages",
            DuplicateResourceBehavior.UseFullName);

        // First wins the short alias; second is reachable via its full base name.
        Assert.True(TranslationSource.Instance.HasResourceManager("messages"));
        Assert.True(TranslationSource.Instance.HasResourceManager("other.resources.messages"));
    }

    [Fact]
    public void Register_RejectsNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LocalizationRegistryBuilder.Register(
                target: null!,
                alias: "messages",
                manager: NewManager(),
                baseName: "MyApp.Resources.Messages",
                behavior: DuplicateResourceBehavior.Throw));
    }

    [Fact]
    public void Register_RejectsNullManager()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LocalizationRegistryBuilder.Register(
                TranslationSource.Instance,
                alias: "messages",
                manager: null!,
                baseName: "MyApp.Resources.Messages",
                behavior: DuplicateResourceBehavior.Throw));
    }

    [Fact]
    public void Register_RejectsBlankAlias()
    {
        Assert.Throws<ArgumentException>(() =>
            LocalizationRegistryBuilder.Register(
                TranslationSource.Instance,
                alias: "   ",
                manager: NewManager(),
                baseName: "MyApp.Resources.Messages",
                behavior: DuplicateResourceBehavior.Throw));
    }

    [Fact]
    public void Register_RejectsBlankBaseName()
    {
        Assert.Throws<ArgumentException>(() =>
            LocalizationRegistryBuilder.Register(
                TranslationSource.Instance,
                alias: "messages",
                manager: NewManager(),
                baseName: "   ",
                behavior: DuplicateResourceBehavior.Throw));
    }
}
