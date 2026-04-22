using LiteDB;

namespace AppSwitcher.Stats.Storage;

internal class AppRegistryDocument
{
    [BsonId]
    public string ProcessName { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public DateTime FirstSeen { get; init; }
}