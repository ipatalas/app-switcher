using AppSwitcher.Stats.Storage;
using LiteDB;

namespace AppSwitcher.Stats.Migrations;

/// <summary>
/// Removes the bucket stored under the default date (0001-01-01), which could be flushed by an untouched
/// session snapshot before the first event of the day was recorded.
/// </summary>
internal class RemoveDefaultDateBucketMigration : IStatsMigration
{
    public int Version => 2;

    public void Up(ILiteDatabase db)
    {
        var col = db.GetCollection(DailyBucketDocument.CollectionName);
        col.Delete(new BsonValue(default(DateOnly).ToString("yyyy-MM-dd")));
    }
}
