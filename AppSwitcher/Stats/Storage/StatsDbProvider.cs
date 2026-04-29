using LiteDB;
using System.IO;

namespace AppSwitcher.Stats.Storage;

public class StatsDbProvider(string dbPath, Func<ILiteDatabase> factory)
{
    /// <summary>
    /// Gets a fresh database each time it's called
    /// </summary>
    public ILiteDatabase Get() => factory();

    public bool Exists() => File.Exists(dbPath);

    public void Delete()
    {
        File.Delete(dbPath);
    }
}