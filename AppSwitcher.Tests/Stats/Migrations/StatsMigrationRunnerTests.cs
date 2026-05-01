using System.IO;
using AppSwitcher.Configuration.Storage;
using AppSwitcher.Stats.Migrations;
using AppSwitcher.Stats.Storage;
using AwesomeAssertions;
using LiteDB;
using LiteDB.Engine;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppSwitcher.Tests.Stats.Migrations;

public class StatsMigrationRunnerTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly StatsDbProvider _dbProvider;

    public StatsMigrationRunnerTests()
    {
        BsonMapper.Global.EnumAsInteger = true;
        BsonMapper.Global.RegisterType<DateOnly>(
            serialize: d => d.ToString("yyyy-MM-dd"),
            deserialize: v => DateOnly.Parse(v.AsString));

        _db = new LiteDatabase(":memory:");

        // StatsMigrationRunner disposes the ILiteDatabase returned by the factory (using var db = ...).
        // Wrap _db in a non-disposing shell so the test can still query it after RunPending() returns.
        // StatsDbProvider.Exists() checks File.Exists — pass the test assembly path so it returns true.
        var existingPath = typeof(StatsMigrationRunnerTests).Assembly.Location;
        _dbProvider = new StatsDbProvider(existingPath, () => new NonDisposingWrapper(_db));
    }

    public void Dispose() => _db.Dispose();

    private StatsMigrationRunner BuildRunner(params IStatsMigration[] migrations) =>
        new(_dbProvider, migrations, NullLogger<StatsMigrationRunner>.Instance);

    // ── RunPending ─────────────────────────────────────────────────────────────

    [Fact]
    public void RunPending_ReturnsTrue_WhenNoMigrationsRegistered()
    {
        var sut = BuildRunner();

        var result = sut.RunPending();

        result.Should().BeTrue();
    }

    [Fact]
    public void RunPending_RunsMigration_WhenNotYetExecuted()
    {
        var migration = new FakeMigration(version: 1);
        var sut = BuildRunner(migration);

        sut.RunPending();

        migration.WasRun.Should().BeTrue();
    }

    [Fact]
    public void RunPending_RecordsMigrationVersion_AfterSuccessfulRun()
    {
        var migration = new FakeMigration(version: 1);
        var sut = BuildRunner(migration);
        sut.RunPending();

        // Run again — migration must be skipped because version was recorded
        sut.RunPending();

        migration.RunCount.Should().Be(1);
    }

    [Fact]
    public void RunPending_DoesNotRecordVersion_WhenMigrationThrows()
    {
        var failing = new FakeMigration(version: 1, throws: true);
        BuildRunner(failing).RunPending();

        // Run again with a non-throwing migration of the same version —
        // it must run again because the failed run should not have recorded the version
        var retry = new FakeMigration(version: 1);
        BuildRunner(retry).RunPending();

        retry.WasRun.Should().BeTrue();
    }

    [Fact]
    public void RunPending_SkipsMigration_WhenAlreadyRecorded()
    {
        // Pre-seed the migration record directly into the shared DB
        _db.GetCollection<MigrationRecord>("_migrations")
           .Insert(new MigrationRecord { Version = 1, ExecutedAt = DateTime.UtcNow });

        var migration = new FakeMigration(version: 1);
        BuildRunner(migration).RunPending();

        migration.WasRun.Should().BeFalse();
    }

    [Fact]
    public void RunPending_RunsMigrationsInVersionOrder()
    {
        var order = new List<int>();
        var m3 = new FakeMigration(version: 3, onRun: () => order.Add(3));
        var m1 = new FakeMigration(version: 1, onRun: () => order.Add(1));
        var m2 = new FakeMigration(version: 2, onRun: () => order.Add(2));
        var sut = BuildRunner(m3, m1, m2);

        sut.RunPending();

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void RunPending_ReturnsTrue_WhenAllMigrationsSucceed()
    {
        var sut = BuildRunner(new FakeMigration(1), new FakeMigration(2));

        var result = sut.RunPending();

        result.Should().BeTrue();
    }

    [Fact]
    public void RunPending_ReturnsFalse_WhenMigrationThrows()
    {
        var sut = BuildRunner(new FakeMigration(version: 1, throws: true));

        var result = sut.RunPending();

        result.Should().BeFalse();
    }

    [Fact]
    public void RunPending_StopsAfterFailure_DoesNotRunSubsequentMigrations()
    {
        var m1 = new FakeMigration(version: 1, throws: true);
        var m2 = new FakeMigration(version: 2);
        BuildRunner(m1, m2).RunPending();

        m2.WasRun.Should().BeFalse();
    }

    [Fact]
    public void RunPending_ReturnsTrue_WhenDbDoesNotExist()
    {
        var nonExistentProvider = new StatsDbProvider(
            @"C:\does\not\exist\stats.db",
            () => throw new InvalidOperationException("Should not open DB"));

        var sut = new StatsMigrationRunner(
            nonExistentProvider,
            [new FakeMigration(1)],
            NullLogger<StatsMigrationRunner>.Instance);

        var result = sut.RunPending();

        result.Should().BeTrue();
    }

    // ── Fakes ──────────────────────────────────────────────────────────────────

    private sealed class FakeMigration(int version, bool throws = false, Action? onRun = null) : IStatsMigration
    {
        public int Version => version;
        public bool WasRun => RunCount > 0;
        public int RunCount { get; private set; }

        public void Up(ILiteDatabase db)
        {
            RunCount++;
            onRun?.Invoke();
            if (throws)
                throw new InvalidOperationException("Simulated migration failure");
        }
    }

    /// <summary>
    /// Delegates all ILiteDatabase calls to the inner instance but ignores Dispose(),
    /// allowing the test to keep querying the in-memory DB after RunPending() returns.
    /// </summary>
    private sealed class NonDisposingWrapper(ILiteDatabase inner) : ILiteDatabase
    {
        public void Dispose() { /* intentionally no-op */ }

        public BsonMapper Mapper => inner.Mapper;
        public ILiteStorage<string> FileStorage => inner.FileStorage;
        public int UserVersion { get => inner.UserVersion; set => inner.UserVersion = value; }
        public TimeSpan Timeout { get => inner.Timeout; set => inner.Timeout = value; }
        public bool UtcDate { get => inner.UtcDate; set => inner.UtcDate = value; }
        public long LimitSize { get => inner.LimitSize; set => inner.LimitSize = value; }
        public int CheckpointSize { get => inner.CheckpointSize; set => inner.CheckpointSize = value; }
        public Collation Collation => inner.Collation;

        public ILiteCollection<BsonDocument> GetCollection(string name, BsonAutoId autoId = BsonAutoId.ObjectId) => inner.GetCollection(name, autoId);
        public ILiteCollection<T> GetCollection<T>() => inner.GetCollection<T>();
        public ILiteCollection<T> GetCollection<T>(BsonAutoId autoId = BsonAutoId.ObjectId) => inner.GetCollection<T>(autoId);
        public ILiteCollection<T> GetCollection<T>(string name, BsonAutoId autoId = BsonAutoId.ObjectId) => inner.GetCollection<T>(name, autoId);
        public bool BeginTrans() => inner.BeginTrans();
        public bool Commit() => inner.Commit();
        public bool Rollback() => inner.Rollback();
        public ILiteStorage<TFileId> GetStorage<TFileId>(string filesCollection = "_files", string chunksCollection = "_chunks") => inner.GetStorage<TFileId>(filesCollection, chunksCollection);
        public IEnumerable<string> GetCollectionNames() => inner.GetCollectionNames();
        public bool CollectionExists(string name) => inner.CollectionExists(name);
        public bool DropCollection(string name) => inner.DropCollection(name);
        public bool RenameCollection(string oldName, string newName) => inner.RenameCollection(oldName, newName);
        public IBsonDataReader Execute(TextReader commandReader, BsonDocument? parameters = null) => inner.Execute(commandReader, parameters);
        public IBsonDataReader Execute(string command, BsonDocument? parameters = null) => inner.Execute(command, parameters);
        public IBsonDataReader Execute(string command, params BsonValue[] args) => inner.Execute(command, args);
        public void Checkpoint() => inner.Checkpoint();
        public long Rebuild(RebuildOptions? options = null) => inner.Rebuild(options);
        public BsonValue Pragma(string name) => inner.Pragma(name);
        public BsonValue Pragma(string name, BsonValue value) => inner.Pragma(name, value);
    }
}
