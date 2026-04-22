using LiteDB;

namespace AppSwitcher.Stats.Storage;

public class StatsDbProvider(Func<ILiteDatabase> factory)
{
    /// <summary>
    /// Gets a fresh database each time it's called
    /// </summary>
    public ILiteDatabase Get() => factory();
}