using AppSwitcher.Stats.Storage;
using LiteDB;

namespace AppSwitcher.Stats.Migrations;

internal class DateTimeToDateOnlyMigration(TimeProvider timeProvider) : IStatsMigration
{
    public int Version => 1;

    public void Up(ILiteDatabase db)
    {
        var col = db.GetCollection(DailyBucketDocument.CollectionName);
        var docs = col.FindAll().ToList();
        var zone = timeProvider.LocalTimeZone;

        foreach (var doc in docs)
        {
            var oldId = doc["_id"];

            if (oldId.Type != BsonType.DateTime)
            {
                continue;
            }

            // LiteDB reads DateTime as local time, but we want to interpret it as UTC
            var dt = DateTime.SpecifyKind(oldId.AsDateTime.ToUniversalTime(), DateTimeKind.Utc);
            // manually convert UTC -> Local using provided zone so that it works in production and in local/CI tests
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(dt, zone).Date;
            var newId = new BsonValue(DateOnly.FromDateTime(localDate).ToString("yyyy-MM-dd"));

            doc["_id"] = newId;

            col.Delete(oldId);
            col.Insert(doc);
        }
    }
}