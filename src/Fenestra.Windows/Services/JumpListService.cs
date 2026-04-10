using Fenestra.Windows.Models;
using Fenestra.Windows.Native;
using Fenestra.Windows.Native.Interfaces;
using Fenestra.Windows.Native.Structs;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Fenestra.Windows.Services;

/// <summary>
/// Default <see cref="IJumpListCategory"/> implementation. Holds buffered files and
/// tasks for a single named category until <see cref="IJumpListService.Apply"/> commits
/// everything to the shell.
/// </summary>
internal sealed class JumpListCategory : IJumpListCategory
{
    private readonly object _lock = new();
    private readonly List<string> _files = new();
    private readonly List<JumpListTask> _tasks = new();

    public JumpListCategory(string name) => Name = name;

    public string Name { get; }

    public IJumpListCategory AddFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return this;
        lock (_lock)
        {
            _files.Remove(path);
            _files.Add(path);
        }
        return this;
    }

    public IJumpListCategory AddTask(JumpListTask task)
    {
        if (task is null) return this;
        lock (_lock) _tasks.Add(task);
        return this;
    }

    public IReadOnlyList<string> Files { get { lock (_lock) return _files.ToArray(); } }
    public IReadOnlyList<JumpListTask> Tasks { get { lock (_lock) return _tasks.ToArray(); } }

    public void Clear()
    {
        lock (_lock)
        {
            _files.Clear();
            _tasks.Clear();
        }
    }

    internal (string[] files, JumpListTask[] tasks) Snapshot()
    {
        lock (_lock) return (_files.ToArray(), _tasks.ToArray());
    }
}

/// <summary>
/// Default <see cref="IJumpListService"/> implementation backed by Win32
/// <c>ICustomDestinationList</c> COM. No WPF dependency.
/// </summary>
internal sealed class JumpListService : IJumpListService
{
    private readonly object _lock = new();
    private readonly IAumidRegistrationManager _registration;
    private readonly List<JumpListCategory> _categories = new();
    private readonly List<JumpListTask> _userTasks = new();

    public JumpListService(IAumidRegistrationManager registration)
    {
        _registration = registration;
        try { _registration.EnsureRegistered(); }
        catch (Exception ex) { Debug.WriteLine($"[Fenestra.JumpList] EnsureRegistered failed on init: {ex.Message}"); }
    }

    public IJumpListCategory AddCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name must not be empty.", nameof(name));

        lock (_lock)
        {
            var existing = _categories.Find(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) return existing;

            var category = new JumpListCategory(name);
            _categories.Add(category);
            return category;
        }
    }

    public void RemoveCategory(string name)
    {
        lock (_lock)
            _categories.RemoveAll(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IJumpListCategory> Categories
    {
        get { lock (_lock) return _categories.ToArray(); }
    }

    public void AddRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try { _registration.EnsureRegistered(); }
        catch (Exception ex) { Debug.WriteLine($"[Fenestra.JumpList] EnsureRegistered failed: {ex.Message}"); }

        var pathPtr = Marshal.StringToCoTaskMemUni(path);
        try
        {
            SHAddToRecentDocs(SHARD_PATHW, pathPtr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pathPtr);
        }
    }

    public void SetTasks(params JumpListTask[] tasks)
    {
        lock (_lock)
        {
            _userTasks.Clear();
            if (tasks is { Length: > 0 })
                _userTasks.AddRange(tasks);
        }
    }

    public void DeleteList()
    {
        lock (_lock)
        {
            _categories.Clear();
            _userTasks.Clear();
        }

        try
        {
            var destList = CreateDestinationList();
            if (destList is null) return;
            try { destList.DeleteList(null); }
            finally { Marshal.ReleaseComObject(destList); }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Fenestra.JumpList] DeleteList failed: {ex.Message}");
        }
    }

    public void Apply()
    {
        Platform.EnsureWindows();
        _registration.EnsureRegistered();

        JumpListTask[] userTaskSnapshot;
        JumpListCategory[] categorySnapshot;
        lock (_lock)
        {
            userTaskSnapshot = _userTasks.ToArray();
            categorySnapshot = _categories.ToArray();
        }

        var destList = CreateDestinationList();
        if (destList is null) return;

        try
        {
            if (!BeginList(destList, out var maxSlots))
                return;

            try
            {
                var defaultExePath = CurrentExecutablePath.Get();
                AppendCategories(destList, categorySnapshot, defaultExePath, maxSlots);
                AppendUserTasks(destList, userTaskSnapshot, defaultExePath, maxSlots);
                CommitList(destList);
            }
            catch
            {
                destList.AbortList();
                throw;
            }
        }
        finally
        {
            Marshal.ReleaseComObject(destList);
        }
    }

    // ── Apply pipeline steps ────────────────────────────────────────────

    private static ICustomDestinationList? CreateDestinationList()
    {
        var clsid = JumpListClsids.DestinationList;
        var iid = JumpListClsids.IID_ICustomDestinationList;
        var hr = ShellNativeMethods.CoCreateInstance(
            ref clsid, IntPtr.Zero, ShellNativeMethods.CLSCTX_INPROC_SERVER, ref iid, out var obj);
        if (hr < 0 || obj is not ICustomDestinationList destList)
        {
            Debug.WriteLine($"[Fenestra.JumpList] CoCreateInstance(DestinationList) failed: 0x{hr:X8}");
            return null;
        }
        return destList;
    }

    private static bool BeginList(ICustomDestinationList destList, out uint maxSlots)
    {
        var removedIid = JumpListClsids.IID_IObjectArray;
        var hr = destList.BeginList(out maxSlots, ref removedIid, out var removedPtr);

        // Always release the removed-items array, regardless of success or failure.
        if (removedPtr != IntPtr.Zero) Marshal.Release(removedPtr);

        if (hr < 0)
        {
            Debug.WriteLine($"[Fenestra.JumpList] BeginList failed: 0x{hr:X8}");
            destList.AbortList();
            return false;
        }
        return true;
    }

    private static void AppendCategories(
        ICustomDestinationList destList,
        JumpListCategory[] categories,
        string defaultExePath,
        uint maxSlots)
    {
        foreach (var category in categories)
        {
            var (files, tasks) = category.Snapshot();
            var collection = BuildCategoryCollection(files, tasks, defaultExePath, maxSlots);
            if (collection is null) continue;

            try
            {
                var hr = destList.AppendCategory(category.Name, collection);
                if (hr < 0)
                    Debug.WriteLine($"[Fenestra.JumpList] AppendCategory('{category.Name}') failed: 0x{hr:X8}");
            }
            finally
            {
                Marshal.ReleaseComObject(collection);
            }
        }
    }

    private static void AppendUserTasks(
        ICustomDestinationList destList,
        JumpListTask[] tasks,
        string defaultExePath,
        uint maxSlots)
    {
        var collection = BuildTaskCollection(tasks, defaultExePath, maxSlots);
        if (collection is null) return;

        try
        {
            var hr = destList.AddUserTasks(collection);
            if (hr < 0)
                Debug.WriteLine($"[Fenestra.JumpList] AddUserTasks failed: 0x{hr:X8}");
        }
        finally
        {
            Marshal.ReleaseComObject(collection);
        }
    }

    private static void CommitList(ICustomDestinationList destList)
    {
        var hr = destList.CommitList();
        if (hr < 0)
            Debug.WriteLine($"[Fenestra.JumpList] CommitList failed: 0x{hr:X8}");
    }

    // ── Collection builders ─────────────────────────────────────────────

    private static IObjectArray? BuildCategoryCollection(
        string[] files, JumpListTask[] tasks, string defaultExePath, uint maxSlots)
    {
        if (files.Length == 0 && tasks.Length == 0) return null;

        var collection = CreateObjectCollection();
        if (collection is null) return null;

        var added = AddFilesToCollection(collection, files, maxSlots);
        added = AddTasksToCollection(collection, tasks, defaultExePath, maxSlots, added);

        if (added == 0)
        {
            Marshal.ReleaseComObject(collection);
            return null;
        }

        return (IObjectArray)collection;
    }

    private static IObjectArray? BuildTaskCollection(
        JumpListTask[] tasks, string defaultExePath, uint maxSlots)
    {
        if (tasks.Length == 0) return null;

        var collection = CreateObjectCollection();
        if (collection is null) return null;

        var added = AddTasksToCollection(collection, tasks, defaultExePath, maxSlots);

        if (added == 0)
        {
            Marshal.ReleaseComObject(collection);
            return null;
        }

        return (IObjectArray)collection;
    }

    private static uint AddFilesToCollection(
        IObjectCollection collection, string[] files, uint maxSlots, uint alreadyAdded = 0)
    {
        var added = alreadyAdded;
        foreach (var path in files)
        {
            if (added >= maxSlots) break;
            if (!File.Exists(path)) continue;

            var link = CreateShellLinkForFile(path);
            if (link is null) continue;

            if (collection.AddObject(link) >= 0) added++;
            Marshal.ReleaseComObject(link);
        }
        return added;
    }

    private static uint AddTasksToCollection(
        IObjectCollection collection, JumpListTask[] tasks, string defaultExePath,
        uint maxSlots, uint alreadyAdded = 0)
    {
        var added = alreadyAdded;
        foreach (var task in tasks)
        {
            if (added >= maxSlots) break;

            var link = CreateShellLinkForTask(task, defaultExePath);
            if (link is null) continue;

            if (collection.AddObject(link) >= 0) added++;
            Marshal.ReleaseComObject(link);
        }
        return added;
    }

    // ── ShellLink helpers ───────────────────────────────────────────────

    private static IObjectCollection? CreateObjectCollection()
    {
        var clsid = JumpListClsids.EnumerableObjectCollection;
        var iid = JumpListClsids.IID_IObjectCollection;
        var hr = ShellNativeMethods.CoCreateInstance(
            ref clsid, IntPtr.Zero, ShellNativeMethods.CLSCTX_INPROC_SERVER, ref iid, out var obj);
        if (hr < 0 || obj is not IObjectCollection coll)
        {
            Debug.WriteLine($"[Fenestra.JumpList] CoCreateInstance(ObjectCollection) failed: 0x{hr:X8}");
            return null;
        }
        return coll;
    }

    private static IShellLinkW? CreateShellLinkForTask(JumpListTask task, string defaultExePath)
    {
        if (string.IsNullOrEmpty(task.Title))
        {
            Debug.WriteLine("[Fenestra.JumpList] Skipping task with empty Title");
            return null;
        }

        var link = CreateRawShellLink();
        if (link is null) return null;

        try
        {
            var exePath = string.IsNullOrEmpty(task.ApplicationPath) ? defaultExePath : task.ApplicationPath!;
            link.SetPath(exePath);

            if (!string.IsNullOrEmpty(task.Arguments))
                link.SetArguments(task.Arguments!);

            if (!string.IsNullOrEmpty(task.Description))
                link.SetDescription(task.Description!);

            var workDir = task.WorkingDirectory
                ?? Path.GetDirectoryName(exePath)
                ?? string.Empty;
            if (!string.IsNullOrEmpty(workDir))
                link.SetWorkingDirectory(workDir);

            if (!string.IsNullOrEmpty(task.IconResourcePath))
                link.SetIconLocation(task.IconResourcePath!, task.IconResourceIndex);
            else
                link.SetIconLocation(exePath, 0);

            if (link is not IPropertyStore ps)
            {
                Debug.WriteLine("[Fenestra.JumpList] ShellLink does not implement IPropertyStore");
                Marshal.ReleaseComObject(link);
                return null;
            }

            SetTitleProperty(ps, task.Title);
            ps.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Fenestra.JumpList] Failed to configure task '{task.Title}': {ex.Message}");
            Marshal.ReleaseComObject(link);
            return null;
        }

        return link;
    }

    private static IShellLinkW? CreateShellLinkForFile(string filePath)
    {
        var link = CreateRawShellLink();
        if (link is null) return null;

        try
        {
            link.SetPath(filePath);
            link.SetDescription(filePath);

            if (link is not IPropertyStore ps)
            {
                Debug.WriteLine("[Fenestra.JumpList] ShellLink does not implement IPropertyStore");
                Marshal.ReleaseComObject(link);
                return null;
            }

            SetTitleProperty(ps, Path.GetFileName(filePath));
            ps.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Fenestra.JumpList] Failed to configure file '{filePath}': {ex.Message}");
            Marshal.ReleaseComObject(link);
            return null;
        }

        return link;
    }

    private static IShellLinkW? CreateRawShellLink()
    {
        var clsid = JumpListClsids.ShellLink;
        var iid = JumpListClsids.IID_IShellLinkW;
        var hr = ShellNativeMethods.CoCreateInstance(
            ref clsid, IntPtr.Zero, ShellNativeMethods.CLSCTX_INPROC_SERVER, ref iid, out var obj);
        if (hr < 0 || obj is not IShellLinkW link)
        {
            Debug.WriteLine($"[Fenestra.JumpList] CoCreateInstance(ShellLink) failed: 0x{hr:X8}");
            return null;
        }
        return link;
    }

    private static void SetTitleProperty(IPropertyStore store, string title)
    {
        const ushort VT_LPWSTR = 31;
        var strPtr = Marshal.StringToCoTaskMemUni(title);
        try
        {
            var propvar = new PROPVARIANT { vt = VT_LPWSTR, p = strPtr };
            var key = PKEY_Title;
            var hr = store.SetValue(ref key, propvar);
            if (hr < 0)
                Debug.WriteLine($"[Fenestra.JumpList] SetValue(Title) failed: 0x{hr:X8}");
        }
        finally
        {
            Marshal.FreeCoTaskMem(strPtr);
        }
    }

    private static readonly PROPERTYKEY PKEY_Title = new()
    {
        fmtid = new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"),
        pid = 2,
    };

    // ── P/Invoke ────────────────────────────────────────────────────────
    private const uint SHARD_PATHW = 0x00000003;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHAddToRecentDocs(uint uFlags, IntPtr pv);
}
