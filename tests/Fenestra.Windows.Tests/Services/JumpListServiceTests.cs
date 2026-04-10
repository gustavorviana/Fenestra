using Fenestra.Windows.Models;
using Fenestra.Windows.Services;

namespace Fenestra.Windows.Tests.Services;

public class JumpListServiceTests
{
    private sealed class NoOpRegistrationManager : IAumidRegistrationManager
    {
        public int EnsureRegisteredCallCount { get; private set; }
        public void EnsureRegistered() => EnsureRegisteredCallCount++;
        public bool NeedsRegistration() => false;
    }

    [Fact]
    public void Ctor_CallsEnsureRegisteredOnce()
    {
        var reg = new NoOpRegistrationManager();
        _ = new JumpListService(reg);
        Assert.Equal(1, reg.EnsureRegisteredCallCount);
    }

    // --- Categories ---

    [Fact]
    public void AddCategory_ReturnsSameInstanceForSameName()
    {
        var service = new JumpListService(new NoOpRegistrationManager());

        var a = service.AddCategory("Recent");
        var b = service.AddCategory("Recent");

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCategory_IsCaseInsensitive()
    {
        var service = new JumpListService(new NoOpRegistrationManager());

        var a = service.AddCategory("recent");
        var b = service.AddCategory("RECENT");

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCategory_WithEmptyName_Throws()
    {
        var service = new JumpListService(new NoOpRegistrationManager());

        Assert.Throws<ArgumentException>(() => service.AddCategory(""));
    }

    [Fact]
    public void RemoveCategory_RemovesExisting()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        service.AddCategory("Projects");

        service.RemoveCategory("Projects");

        Assert.Empty(service.Categories);
    }

    [Fact]
    public void RemoveCategory_NonExistent_IsNoOp()
    {
        var service = new JumpListService(new NoOpRegistrationManager());

        var ex = Record.Exception(() => service.RemoveCategory("Nope"));

        Assert.Null(ex);
    }

    [Fact]
    public void Categories_ReturnsInsertionOrder()
    {
        var service = new JumpListService(new NoOpRegistrationManager());

        service.AddCategory("Alpha");
        service.AddCategory("Beta");
        service.AddCategory("Gamma");

        var names = service.Categories.Select(c => c.Name).ToArray();
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, names);
    }

    // --- IJumpListCategory ---

    [Fact]
    public void Category_AddFile_Accumulates()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        var cat = service.AddCategory("Docs");

        cat.AddFile("a.txt").AddFile("b.txt");

        Assert.Equal(new[] { "a.txt", "b.txt" }, cat.Files);
    }

    [Fact]
    public void Category_AddFile_DeduplicatesKeepingLast()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        var cat = service.AddCategory("Docs");

        cat.AddFile("a.txt").AddFile("b.txt").AddFile("a.txt");

        Assert.Equal(new[] { "b.txt", "a.txt" }, cat.Files);
    }

    [Fact]
    public void Category_AddTask_Accumulates()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        var cat = service.AddCategory("Actions");

        cat.AddTask(new JumpListTask { Title = "T1" })
           .AddTask(new JumpListTask { Title = "T2" });

        Assert.Equal(2, cat.Tasks.Count);
    }

    [Fact]
    public void Category_Clear_EmptiesFilesAndTasks()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        var cat = service.AddCategory("Mixed");
        cat.AddFile("x.txt").AddTask(new JumpListTask { Title = "Y" });

        cat.Clear();

        Assert.Empty(cat.Files);
        Assert.Empty(cat.Tasks);
    }

    // --- User tasks ---

    [Fact]
    public void SetTasks_ReplacesBufferedTasks()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        service.SetTasks(new JumpListTask { Title = "A" });
        service.SetTasks(new JumpListTask { Title = "B" });

        // No way to read user tasks directly, but should not throw.
        var ex = Record.Exception(() => service.SetTasks(new JumpListTask { Title = "C" }));
        Assert.Null(ex);
    }

    // --- DeleteList ---

    [Fact]
    public void DeleteList_ClearsBufferAndDoesNotThrow()
    {
        var service = new JumpListService(new NoOpRegistrationManager());
        service.AddCategory("X").AddFile("y.txt");
        service.SetTasks(new JumpListTask { Title = "Z" });

        var ex = Record.Exception(() => service.DeleteList());

        Assert.Null(ex);
        Assert.Empty(service.Categories);
    }
}
