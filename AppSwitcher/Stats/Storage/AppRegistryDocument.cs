using LiteDB;

namespace AppSwitcher.Stats.Storage;

internal class AppRegistryDocument
{
    public const string CollectionName = "app_registry";

    [BsonId]
    public string ProcessName { get; init; } = "";

    public string DisplayName { get; init; } = "";
}