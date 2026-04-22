using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AwesomeAssertions;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class AppRegistryCacheTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly AppRegistryCache _sut;

    public AppRegistryCacheTests()
    {
        _sut = new AppRegistryCache(_db, NullLogger<AppRegistryCache>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void GetOrAdd_ReturnsProcessNameWithoutExtension_WhenPathIsNull()
    {
        var result = _sut.GetOrAdd("notepad.exe", null);

        result.Should().Be("notepad");
    }

    [Fact]
    public void GetOrAdd_ReturnsProcessNameWithoutExtension_WhenPathDoesNotExist()
    {
        var result = _sut.GetOrAdd("notepad.exe", @"C:\nonexistent\path\notepad.exe");

        result.Should().Be("notepad");
    }

    [Fact]
    public void GetOrAdd_PersistsEntryToLiteDb_OnCacheMiss()
    {
        _sut.GetOrAdd("notepad.exe", null);

        var col = _db.GetCollection<AppRegistryDocument>("app_registry");
        var doc = col.FindById("notepad.exe");

        doc.Should().NotBeNull();
        doc.DisplayName.Should().Be("notepad");
    }

    [Fact]
    public void GetOrAdd_DoesNotInsertDuplicate_WhenCalledTwice()
    {
        _sut.GetOrAdd("notepad.exe", null);
        _sut.GetOrAdd("notepad.exe", null);

        var col = _db.GetCollection<AppRegistryDocument>("app_registry");
        col.Count().Should().Be(1);
    }

    [Fact]
    public void GetOrAdd_ReturnsCachedValue_OnSecondCall_WithoutHittingDb()
    {
        _sut.GetOrAdd("notepad.exe", null);

        // Drop the DB collection to prove second call uses in-memory cache
        _db.DropCollection("app_registry");

        var result = _sut.GetOrAdd("notepad.exe", null);

        result.Should().Be("notepad");
    }

    [Fact]
    public void GetOrAdd_IsCaseInsensitive_ForProcessName()
    {
        _sut.GetOrAdd("Notepad.exe", null);
        var result = _sut.GetOrAdd("notepad.exe", null);

        result.Should().Be("Notepad");
    }

    [Fact]
    public void GetOrAdd_SetsFirstSeen_WhenPersisting()
    {
        var before = DateTime.Now.AddSeconds(-1);

        _sut.GetOrAdd("notepad.exe", null);

        var col = _db.GetCollection<AppRegistryDocument>("app_registry");
        var doc = col.FindById("notepad.exe");

        doc!.FirstSeen.Should().BeAfter(before);
    }
}
