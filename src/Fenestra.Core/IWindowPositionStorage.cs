using Fenestra.Core.Models;

namespace Fenestra.Core;

public interface IWindowPositionStorage
{
    WindowPositionData? Load(string key);
    void Save(string key, WindowPositionData data);
    void Delete(string key);
}
