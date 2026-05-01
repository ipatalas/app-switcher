using AppSwitcher.Stats.Migrations;
using AppSwitcher.Stats.Storage;
using AwesomeAssertions;
using LiteDB;
using Xunit;

namespace AppSwitcher.Tests.Stats.Migrations;

public class DateTimeToDateOnlyMigrationTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly DateTimeToDateOnlyMigration _sut = new();

    public DateTimeToDateOnlyMigrationTests()
    {
        BsonMapper.Global.EnumAsInteger = true;
        BsonMapper.Global.RegisterType(
            serialize: d => d.ToString("yyyy-MM-dd"),
            deserialize: v => DateOnly.Parse(v.AsString));
        _db = new LiteDatabase(":memory:");
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Up_ConvertsDateTimeId_ToDateOnlyString()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        var utcDate = new DateTime(2026, 3, 15, 22, 0, 0, DateTimeKind.Utc);
        var doc = new BsonDocument
        {
            ["_id"] = new BsonValue(utcDate),
            ["TotalSwitches"] = 5,
        };
        col.Insert(doc);

        _sut.Up(_db);

        var result = col.FindAll().ToList();
        result.Should().HaveCount(1);
        result[0]["_id"].Type.Should().Be(BsonType.String);
        result[0]["TotalSwitches"].AsInt32.Should().Be(5);
    }

    [Fact]
    public void Up_PreservesLocalCalendarDate_WhenUtcCrossesDateBoundary()
    {
        var targetTimeZone = TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), null, null);
        // UTC 2026-04-15T22:00 = local 2026-04-16T00:00 in UTC+2
        // The migration uses ToLocalTime().Date so the stored key reflects local date
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        var utcDate = new DateTime(2026, 4, 15, 22, 0, 0, DateTimeKind.Utc);
        var doc = new BsonDocument { ["_id"] = new BsonValue(utcDate) };
        col.Insert(doc);

        _sut.Up(_db);

        var result = col.FindAll().Single();
        var storedId = result["_id"].AsString;
        var expectedDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, targetTimeZone).Date;
        storedId.Should().Be(expectedDate.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void Up_SkipsDocuments_WhenIdIsAlreadyString()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        var doc = new BsonDocument
        {
            ["_id"] = new BsonValue("2026-03-15"),
            ["TotalSwitches"] = 3,
        };
        col.Insert(doc);

        _sut.Up(_db);

        var result = col.FindAll().Single();
        result["_id"].AsString.Should().Be("2026-03-15");
        result["TotalSwitches"].AsInt32.Should().Be(3);
    }

    [Fact]
    public void Up_HandlesEmptyCollection_WithoutError()
    {
        _db.GetCollection(DailyBucketDocument.CollectionName);

        var act = () => _sut.Up(_db);

        act.Should().NotThrow();
    }

    [Fact]
    public void Up_ConvertsMultipleDocuments_AllWithDateTimeIds()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        col.Insert(MakeDoc(1));
        col.Insert(MakeDoc(2));
        col.Insert(MakeDoc(3));

        _sut.Up(_db);

        var results = col.FindAll().ToList();
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(d => d["_id"].Type.Should().Be(BsonType.String));

        BsonDocument MakeDoc(int day)
        {
            return new BsonDocument { ["_id"] = new BsonValue(new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc)), ["TotalSwitches"] = day };
        }
    }

    [Fact]
    public void Up_MixedCollection_OnlyConvertsDateTimeIds()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        col.Insert(new BsonDocument { ["_id"] = new BsonValue(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)), ["TotalSwitches"] = 7 });
        col.Insert(new BsonDocument { ["_id"] = new BsonValue("2026-02-11"), ["TotalSwitches"] = 8 });

        _sut.Up(_db);

        var results = col.FindAll().ToList();
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(d => d["_id"].Type.Should().Be(BsonType.String));
    }
}
