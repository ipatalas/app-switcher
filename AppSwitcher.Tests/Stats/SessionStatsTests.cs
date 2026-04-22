using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class SessionStatsTests
{
    private readonly SessionStats _sut = new();

    [Fact]
    public void Snapshot_ReturnsZeroes_WhenNoEventsRecorded()
    {
        var result = _sut.Snapshot(DateTime.Now);

        result.TotalSwitches.Should().Be(0);
        result.TotalTimeSavedMs.Should().Be(0);
        result.TotalPeeks.Should().Be(0);
        result.StaticAppUsage.Should().BeEmpty();
        result.DynamicAppUsage.Should().BeEmpty();
        result.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void RecordSwitch_IncrementsTotalSwitches()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false);
        _sut.RecordSwitch("notepad.exe", null, savedMs: 200, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.TotalSwitches.Should().Be(2);
    }

    [Fact]
    public void RecordSwitch_AccumulatesTotalTimeSavedMs()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 300, isDynamic: false);
        _sut.RecordSwitch("code.exe", null, savedMs: 500, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.TotalTimeSavedMs.Should().Be(800);
    }

    [Fact]
    public void RecordSwitch_TracksStaticAppSwitchCount()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false);
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false);
        _sut.RecordSwitch("code.exe", null, savedMs: 100, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.StaticAppUsage["notepad.exe"].Switches.Should().Be(2);
        result.StaticAppUsage["code.exe"].Switches.Should().Be(1);
        result.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public void RecordSwitch_TracksDynamicAppSwitchCount()
    {
        _sut.RecordSwitch("explorer.exe", null, savedMs: 100, isDynamic: true);
        _sut.RecordSwitch("explorer.exe", null, savedMs: 100, isDynamic: true);

        var result = _sut.Snapshot(DateTime.Now);

        result.DynamicAppUsage["explorer.exe"].Switches.Should().Be(2);
        result.StaticAppUsage.Should().BeEmpty();
    }

    [Fact]
    public void RecordSwitch_KeepsStaticAndDynamicBucketsSeparate()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false);
        _sut.RecordSwitch("explorer.exe", null, savedMs: 100, isDynamic: true);

        var result = _sut.Snapshot(DateTime.Now);

        result.StaticAppUsage.Should().ContainKey("notepad.exe");
        result.StaticAppUsage.Should().NotContainKey("explorer.exe");
        result.DynamicAppUsage.Should().ContainKey("explorer.exe");
        result.DynamicAppUsage.Should().NotContainKey("notepad.exe");
    }

    [Fact]
    public void RecordSwitch_TracksTransitions_WhenPreviousProcessNameProvided()
    {
        _sut.RecordSwitch("code.exe", "notepad.exe", savedMs: 100, isDynamic: false);
        _sut.RecordSwitch("code.exe", "notepad.exe", savedMs: 100, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.Transitions["notepad.exe|code.exe"].Should().Be(2);
    }

    [Fact]
    public void RecordSwitch_DoesNotAddTransition_WhenNoPreviousProcess()
    {
        _sut.RecordSwitch("code.exe", null, savedMs: 100, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.Transitions.Should().BeEmpty();
    }

    [Fact]
    public void RecordPeek_IncrementsTotalPeeks()
    {
        _sut.RecordPeek("notepad.exe", durationMs: 600, isDynamic: false);
        _sut.RecordPeek("notepad.exe", durationMs: 400, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.TotalPeeks.Should().Be(2);
    }

    [Fact]
    public void RecordPeek_AccumulatesPeekTimeInStaticBucket()
    {
        _sut.RecordPeek("notepad.exe", durationMs: 600, isDynamic: false);
        _sut.RecordPeek("notepad.exe", durationMs: 400, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.StaticAppUsage["notepad.exe"].Peeks.Should().Be(2);
        result.StaticAppUsage["notepad.exe"].TotalPeekTimeMs.Should().Be(1000);
        result.DynamicAppUsage.Should().BeEmpty();
    }

    [Fact]
    public void RecordPeek_AccumulatesPeekTimeInDynamicBucket()
    {
        _sut.RecordPeek("explorer.exe", durationMs: 500, isDynamic: true);

        var result = _sut.Snapshot(DateTime.Now);

        result.DynamicAppUsage["explorer.exe"].Peeks.Should().Be(1);
        result.DynamicAppUsage["explorer.exe"].TotalPeekTimeMs.Should().Be(500);
        result.StaticAppUsage.Should().BeEmpty();
    }

    [Fact]
    public void LoadFrom_RestoresAllCounters()
    {
        var doc = new DailyBucketDocument
        {
            Date = DateTime.Now.Date,
            TotalSwitches = 10,
            TotalTimeSavedMs = 5000,
            TotalPeeks = 3,
            StaticAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["code.exe"] = new() { Switches = 7, Peeks = 2, TotalPeekTimeMs = 1200 }
            },
            DynamicAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["explorer.exe"] = new() { Switches = 3 }
            },
            Transitions = new Dictionary<string, int>
            {
                ["notepad.exe|code.exe"] = 4
            }
        };

        _sut.LoadFrom(doc);
        var result = _sut.Snapshot(DateTime.Now);

        result.TotalSwitches.Should().Be(10);
        result.TotalTimeSavedMs.Should().Be(5000);
        result.TotalPeeks.Should().Be(3);
        result.StaticAppUsage["code.exe"].Switches.Should().Be(7);
        result.DynamicAppUsage["explorer.exe"].Switches.Should().Be(3);
        result.Transitions["notepad.exe|code.exe"].Should().Be(4);
    }

    [Fact]
    public void LoadFrom_ThenRecordSwitch_AccumulatesOnTopOfLoaded()
    {
        var doc = new DailyBucketDocument
        {
            Date = DateTime.Now.Date,
            TotalSwitches = 5,
            TotalTimeSavedMs = 2000,
            TotalPeeks = 0,
            StaticAppUsage = [],
            DynamicAppUsage = [],
            Transitions = []
        };

        _sut.LoadFrom(doc);
        _sut.RecordSwitch("notepad.exe", null, savedMs: 500, isDynamic: false);

        var result = _sut.Snapshot(DateTime.Now);

        result.TotalSwitches.Should().Be(6);
        result.TotalTimeSavedMs.Should().Be(2500);
    }

    [Fact]
    public void Snapshot_DateIsDateOnly()
    {
        var now = new DateTime(2026, 4, 21, 15, 30, 0, DateTimeKind.Utc);

        var result = _sut.Snapshot(now);

        result.Date.Should().Be(new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    // ── Alt+Tab ─────────────────────────────────────────────────────────────

    [Fact]
    public void RecordAltTab_IncrementsTotalAltTabSwitchesAndAccumulatesKeyStrokes()
    {
        _sut.RecordAltTab(1);
        _sut.RecordAltTab(3);

        var result = _sut.Snapshot(DateTime.Now);

        result.AltTabSwitches.Should().Be(2);
        result.AltTabKeystrokes.Should().Be(4);
    }

    [Fact]
    public void LoadFrom_RestoresAltTabCounters()
    {
        var doc = new DailyBucketDocument
        {
            Date = DateTime.Now.Date,
            TotalSwitches = 0,
            TotalTimeSavedMs = 0,
            TotalPeeks = 0,
            AltTabSwitches = 7,
            AltTabKeystrokes = 19,
            StaticAppUsage = [],
            DynamicAppUsage = [],
            Transitions = []
        };

        _sut.LoadFrom(doc);
        var result = _sut.Snapshot(DateTime.Now);

        result.AltTabSwitches.Should().Be(7);
        result.AltTabKeystrokes.Should().Be(19);
    }

    // ── Fastest switch ───────────────────────────────────────────────────────

    [Fact]
    public void RecordSwitch_SetsFastestSwitch_OnFirstSwitch()
    {
        _sut.RecordSwitch("spotify.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: 200, letter: "S");

        var result = _sut.Snapshot(DateTime.Now);

        result.FastestSwitch.Should().NotBeNull();
        result.FastestSwitch!.DurationMs.Should().Be(200);
        result.FastestSwitch.AppName.Should().Be("spotify.exe");
        result.FastestSwitch.Letter.Should().Be("S");
    }

    [Fact]
    public void RecordSwitch_UpdatesFastestSwitch_WhenFasterSwitchRecorded()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: 400, letter: "N");
        _sut.RecordSwitch("spotify.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: 150, letter: "S");

        var result = _sut.Snapshot(DateTime.Now);

        result.FastestSwitch!.DurationMs.Should().Be(150);
        result.FastestSwitch.AppName.Should().Be("spotify.exe");
        result.FastestSwitch.Letter.Should().Be("S");
    }

    [Fact]
    public void RecordSwitch_DoesNotUpdateFastestSwitch_WhenSlowerSwitchRecorded()
    {
        _sut.RecordSwitch("spotify.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: 150, letter: "S");
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: 400, letter: "N");

        var result = _sut.Snapshot(DateTime.Now);

        result.FastestSwitch!.DurationMs.Should().Be(150);
        result.FastestSwitch.AppName.Should().Be("spotify.exe");
        result.FastestSwitch.Letter.Should().Be("S");
    }

    [Fact]
    public void RecordSwitch_DoesNotSetFastestSwitch_WhenDurationIsNull()
    {
        _sut.RecordSwitch("notepad.exe", null, savedMs: 100, isDynamic: false,
            fastestDurationMs: null, letter: "N");

        var result = _sut.Snapshot(DateTime.Now);

        result.FastestSwitch.Should().BeNull();
    }

    [Fact]
    public void LoadFrom_RestoresFastestSwitch()
    {
        var doc = new DailyBucketDocument
        {
            Date = DateTime.Now.Date,
            TotalSwitches = 1,
            TotalTimeSavedMs = 100,
            TotalPeeks = 0,
            AltTabSwitches = 0,
            AltTabKeystrokes = 0,
            FastestSwitch = new FastestSwitchRecord { DurationMs = 85, AppName = "spotify.exe", Letter = "S" },
            StaticAppUsage = [],
            DynamicAppUsage = [],
            Transitions = []
        };

        _sut.LoadFrom(doc);
        var result = _sut.Snapshot(DateTime.Now);

        result.FastestSwitch.Should().NotBeNull();
        result.FastestSwitch!.DurationMs.Should().Be(85);
        result.FastestSwitch.AppName.Should().Be("spotify.exe");
        result.FastestSwitch.Letter.Should().Be("S");
    }
}
