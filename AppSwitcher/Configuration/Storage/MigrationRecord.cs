using LiteDB;

namespace AppSwitcher.Configuration.Storage;

internal class MigrationRecord
{
    [BsonId]
    public int Version { get; init; }

    public DateTime ExecutedAt { get; init; }
}