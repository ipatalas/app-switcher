using AppSwitcher.Stats.Storage;
using LiteDB;

namespace AppSwitcher.Stats.Migrations;

internal class DateTimeToDateOnlyMigration : IStatsMigration
{
    public int Version => 1;

    public void Up(ILiteDatabase db)
    {
        var col = db.GetCollection(DailyBucketDocument.CollectionName);
        var docs = col.FindAll().ToList();

        foreach (var doc in docs)
        {
            var oldId = doc["_id"];

            if (oldId.Type != BsonType.DateTime)
            {
                continue;
            }

            var localDate = oldId.AsDateTime.Date;
            var newId = new BsonValue(DateOnly.FromDateTime(localDate).ToString("yyyy-MM-dd"));

            doc["_id"] = newId;

            col.Delete(oldId);
            col.Insert(doc);
        }
    }
}