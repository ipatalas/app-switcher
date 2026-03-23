using AppSwitcher.Configuration.Migrations;
using AppSwitcher.Configuration.Storage;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Configuration.Migrations;

public class MigrationRunnerTests : IDisposable
{
    private readonly LiteDatabase _db;

    public MigrationRunnerTests()
    {
        BsonMapper.Global.EnumAsInteger = true;
        _db = new LiteDatabase(":memory:");
    }

    public void Dispose() => _db.Dispose();

    private MigrationRunner CreateRunner(params IMigration[] migrations) =>
        new(_db, migrations, NullLogger<MigrationRunner>.Instance);

    [Fact]
    public void RunPending_DoesNothing_WhenNoMigrationsRegistered()
    {
        var sut = CreateRunner();

        var act = () => sut.RunPending();

        act.Should().NotThrow();
    }

    [Fact]
    public void RunPending_RunsMigration_WhenNotYetExecuted()
    {
        var migration = new FakeMigration(version: 1);
        var sut = CreateRunner(migration);

        sut.RunPending();

        migration.RunCount.Should().Be(1);
    }

    [Fact]
    public void RunPending_RecordsMigration_AfterSuccessfulRun()
    {
        var migration = new FakeMigration(version: 1);
        var sut = CreateRunner(migration);

        sut.RunPending();

        var records = _db.GetCollection<MigrationRecord>("_migrations").FindAll().ToList();
        records.Should().HaveCount(1);
        records[0].Version.Should().Be(1);
        records[0].ExecutedAt.ToUniversalTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RunPending_RunsMigrationsInVersionOrder()
    {
        var order = new List<int>();
        var m3 = new FakeMigration(version: 3, onUp: () => order.Add(3));
        var m1 = new FakeMigration(version: 1, onUp: () => order.Add(1));
        var m2 = new FakeMigration(version: 2, onUp: () => order.Add(2));
        var sut = CreateRunner(m3, m1, m2);

        sut.RunPending();

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void RunPending_SkipsAlreadyExecutedMigrations()
    {
        var m1 = new FakeMigration(version: 1);
        var sut = CreateRunner(m1);
        sut.RunPending();

        var m2 = new FakeMigration(version: 2);
        sut = CreateRunner(m1, m2);
        sut.RunPending();

        m1.RunCount.Should().Be(1);
        m2.RunCount.Should().Be(1);
    }

    [Fact]
    public void RunPending_RollsBackTransaction_WhenMigrationThrows()
    {
        var failing = new FakeMigration(version: 1, throws: true);
        var sut = CreateRunner(failing);

        var result = sut.RunPending();

        result.Should().BeFalse();
        _db.GetCollection<MigrationRecord>("_migrations").FindAll().Should().BeEmpty();
    }

    [Fact]
    public void RunPending_DoesNotRunSubsequentMigrations_WhenOneFails()
    {
        var m1 = new FakeMigration(version: 1, throws: true);
        var m2 = new FakeMigration(version: 2);
        var sut = CreateRunner(m1, m2);

        var result = sut.RunPending();

        result.Should().BeFalse();
        m2.RunCount.Should().Be(0);
    }
}

file sealed class FakeMigration(int version, bool throws = false, Action? onUp = null) : IMigration
{
    public int Version => version;
    public int RunCount { get; private set; }

    public void Up(LiteDatabase db)
    {
        RunCount++;
        onUp?.Invoke();
        if (throws)
        {
            throw new InvalidOperationException("Migration failed");
        }
    }
}