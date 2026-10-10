using AppSwitcher.Stats.Migrations;
using AppSwitcher.Stats.Storage;
using AwesomeAssertions;
using LiteDB;
using Xunit;

namespace AppSwitcher.Tests.Stats.Migrations;

public class RemoveDefaultDateBucketMigrationTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly RemoveDefaultDateBucketMigration _sut = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Up_RemovesDefaultDateBucket_AndKeepsOthers()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        col.Insert(new BsonDocument { ["_id"] = new BsonValue("0001-01-01"), ["TotalSwitches"] = 0 });
        col.Insert(new BsonDocument { ["_id"] = new BsonValue("2026-04-28"), ["TotalSwitches"] = 30 });

        _sut.Up(_db);

        var result = col.FindAll().Single();
        result["_id"].AsString.Should().Be("2026-04-28");
        result["TotalSwitches"].AsInt32.Should().Be(30);
    }

    [Fact]
    public void Up_DoesNothing_WhenNoDefaultDateBucket()
    {
        var col = _db.GetCollection(DailyBucketDocument.CollectionName);
        col.Insert(new BsonDocument { ["_id"] = new BsonValue("2026-04-28"), ["TotalSwitches"] = 30 });

        _sut.Up(_db);

        col.Count().Should().Be(1);
    }
}
