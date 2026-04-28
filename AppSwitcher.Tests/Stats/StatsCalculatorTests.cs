using System.Windows.Input;
using AppSwitcher.Configuration;
using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AppSwitcher.WindowDiscovery;
using AwesomeAssertions;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Stats;

public class StatsCalculatorTests
{
    private readonly StatsCalculator _sut = new(FakeRegistryCache());

    private static AppConfig EmptyConfig() =>
        new(Modifier: Key.RightCtrl,
            Applications: [],
            PulseBorderEnabled: false,
            Theme: AppThemeSetting.System,
            OverlayEnabled: false,
            OverlayShowDelayMs: 0,
            OverlayKeepOpenWhileModifierHeld: false,
            PeekEnabled: false,
            DynamicModeEnabled: false,
            StatsEnabled: false);

    private static AppRegistryCache FakeRegistryCache(Dictionary<string, string>? displayNames = null)
    {
        var db = new LiteDatabase(":memory:");
        if (displayNames is not null)
        {
            var col = db.GetCollection<AppRegistryDocument>(AppRegistryDocument.CollectionName);
            foreach (var (processName, displayName) in displayNames)
            {
                col.Insert(new AppRegistryDocument { ProcessName = processName, DisplayName = displayName });
            }
        }

        var windowEnumerator = Substitute.For<IWindowEnumerator>();
        windowEnumerator.GetWindows().Returns([]);
        var packagedAppsService = Substitute.For<IPackagedAppsService>();
        packagedAppsService.GetInstalledPaths().Returns(new HashSet<string>());
        var processInspector = Substitute.For<IProcessInspector>();
        var cache = new AppRegistryCache(
            db,
            windowEnumerator,
            packagedAppsService,
            processInspector,
            NullLogger<AppRegistryCache>.Instance);
        cache.Prepopulate(EmptyConfig());
        return cache;
    }

    private static DailyBucketDocument EmptyToday() =>
        new() { Date = DateTime.Today };

    private static DailyBucketDocument BucketOn(DateTime date, int totalSwitches = 0, int altTabSwitches = 0) =>
        new()
        {
            Date = date.Date,
            TotalSwitches = totalSwitches,
            AltTabSwitches = altTabSwitches,
        };

    // ── ComputeLifeGained ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeLifeGained_ReturnsDash_WhenNoData()
    {
        var result = StatsCalculator.ComputeLifeGained([], EmptyToday());

        result.Should().Be("—");
    }

    [Theory]
    [InlineData(30_000,     "30s")]       // 30 seconds
    [InlineData(59_000,     "59s")]       // 59 seconds
    [InlineData(90_000,     "1m 30s")]    // 1 minute 30 seconds
    [InlineData(1_800_000,  "30m")]       // 30 minutes exactly (no trailing 0s)
    [InlineData(3_599_000,  "59m 59s")]   // just under 1 hour
    [InlineData(5_400_000,  "1h 30m")]    // 1 hour 30 minutes
    public void ComputeLifeGained_FormatsCorrectly(int timeSavedMs, string expectedResult)
    {
        var today = new DailyBucketDocument { Date = DateTime.Today, TotalTimeSavedMs = timeSavedMs };

        var result = StatsCalculator.ComputeLifeGained([], today);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ComputeLifeGained_AccumulatesAcrossAllBuckets()
    {
        var buckets = new[]
        {
            new DailyBucketDocument { Date = DateTime.Today.AddDays(-1), TotalTimeSavedMs = 3_600_000 }, // 1h
            new DailyBucketDocument { Date = DateTime.Today.AddDays(-2), TotalTimeSavedMs = 3_600_000 }, // 1h
        };
        var today = new DailyBucketDocument { Date = DateTime.Today, TotalTimeSavedMs = 3_600_000 }; // 1h

        var result = StatsCalculator.ComputeLifeGained(buckets, today);

        result.Should().Be("3h 0m");
    }

    // ── ComputeMuscleMemoGrade ────────────────────────────────────────────────

    [Fact]
    public void ComputeMuscleMemoGrade_ReturnsDash_WhenNoData()
    {
        var result = StatsCalculator.ComputeMuscleMemoGrade([], EmptyToday());

        result.Should().BeNull();
    }

    [Theory]
    // index = (1.0*static + 0.7*dynamic) / (static + dynamic + relapses) * 100
    [InlineData(96, 0, 4,  "S", "Shadow Walker")] // 96/100 * 100 = 96
    [InlineData(85, 0, 15, "A", "Teleporter")]    // 85/100 * 100 = 85
    [InlineData(70, 0, 30, "B", "The Navigator")] // 70/100 * 100 = 70
    [InlineData(50, 0, 50, "C", "Learner")]       // 50/100 * 100 = 50
    [InlineData(30, 0, 70, "D", "Novice")]        // 30/100 * 100 = 30
    [InlineData(0,  0, 100, "F", "Alt-Tabber")]   // 0/100 * 100 = 0
    public void ComputeMuscleMemoGrade_ReturnsCorrectGrade(
        int staticSwitches, int dynamicSwitches, int relapses,
        string expectedGrade, string expectedPersona)
    {
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            AltTabSwitches = relapses,
            StaticAppUsage = staticSwitches > 0
                ? new Dictionary<string, AppUsageStats> { ["app.exe"] = new() { Switches = staticSwitches } }
                : [],
            DynamicAppUsage = dynamicSwitches > 0
                ? new Dictionary<string, AppUsageStats> { ["app.exe"] = new() { Switches = dynamicSwitches } }
                : [],
        };

        var result = StatsCalculator.ComputeMuscleMemoGrade([], today);

        result.Should().Be(new MuscleMemoResult(expectedGrade, expectedPersona));
    }

    // ── ComputeStreak ─────────────────────────────────────────────────────────

    [Fact]
    public void ComputeStreak_ReturnsZero_WhenNoBuckets()
    {
        var result = StatsCalculator.ComputeStreak([], DateTime.Today, _ => true);

        result.Should().Be(0);
    }

    [Fact]
    public void ComputeStreak_ReturnsZero_WhenYesterdayMissing()
    {
        DailyBucketDocument[] buckets =
        [
            BucketOn(DateTime.Today.AddDays(-2), totalSwitches: 25)
        ];

        var result = StatsCalculator.ComputeStreak(buckets, DateTime.Today, b => b.TotalSwitches >= 20);

        result.Should().Be(0);
    }

    [Fact]
    public void ComputeStreak_CountsConsecutiveDays()
    {
        DailyBucketDocument[] buckets =
        [
            BucketOn(DateTime.Today.AddDays(-1), totalSwitches: 25),
            BucketOn(DateTime.Today.AddDays(-2), totalSwitches: 25),
            BucketOn(DateTime.Today.AddDays(-3), totalSwitches: 25)
        ];

        var result = StatsCalculator.ComputeStreak(buckets, DateTime.Today, b => b.TotalSwitches >= 20);

        result.Should().Be(3);
    }

    [Fact]
    public void ComputeStreak_StopsAtGap()
    {
        var buckets = new[]
        {
            BucketOn(DateTime.Today.AddDays(-1), totalSwitches: 25),
            BucketOn(DateTime.Today.AddDays(-2), totalSwitches: 25),
            // gap at -3
            BucketOn(DateTime.Today.AddDays(-4), totalSwitches: 25),
        };

        var result = StatsCalculator.ComputeStreak(buckets, DateTime.Today, b => b.TotalSwitches >= 20);

        result.Should().Be(2);
    }

    [Fact]
    public void ComputeStreak_StopsWhenPredicateFails()
    {
        var buckets = new[]
        {
            BucketOn(DateTime.Today.AddDays(-1), totalSwitches: 25),
            BucketOn(DateTime.Today.AddDays(-2), totalSwitches: 5), // below threshold
            BucketOn(DateTime.Today.AddDays(-3), totalSwitches: 25),
        };

        var result = StatsCalculator.ComputeStreak(buckets, DateTime.Today, b => b.TotalSwitches >= 20);

        result.Should().Be(1);
    }

    // ── ComputeRelapsePct ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeRelapsePct_ReturnsZero_WhenNoSwitches()
    {
        var result = StatsCalculator.ComputeRelapsePct([], EmptyToday());

        result.Should().Be(0);
    }

    [Fact]
    public void ComputeRelapsePct_ComputesCorrectPercentage()
    {
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            TotalSwitches = 80,
            AltTabSwitches = 20,
        };

        var result = StatsCalculator.ComputeRelapsePct([], today);

        result.Should().Be(20);
    }

    // ── IsSoberDay ────────────────────────────────────────────────────────────

    [Fact]
    public void IsSoberDay_ReturnsTrue_WhenNoActivity()
    {
        var result = StatsCalculator.IsSoberDay(EmptyToday());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSoberDay_ReturnsTrue_WhenAltTabUnder5Pct()
    {
        var bucket = new DailyBucketDocument
        {
            Date = DateTime.Today,
            TotalSwitches = 97,
            AltTabSwitches = 3,
        };

        var result = StatsCalculator.IsSoberDay(bucket);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSoberDay_ReturnsFalse_WhenAltTabOver5Pct()
    {
        var bucket = new DailyBucketDocument
        {
            Date = DateTime.Today,
            TotalSwitches = 90,
            AltTabSwitches = 10,
        };

        var result = StatsCalculator.IsSoberDay(bucket);

        result.Should().BeFalse();
    }

    // ── ComputeWastedKeystrokes ───────────────────────────────────────────────

    [Fact]
    public void ComputeWastedKeystrokes_ReturnsZero_WhenNoAltTab()
    {
        var result = StatsCalculator.ComputeWastedKeystrokes([], EmptyToday());

        result.Should().Be(0);
    }

    [Fact]
    public void ComputeWastedKeystrokes_CountsExtraKeystrokes()
    {
        // 5 alt-tab switches but 15 keystrokes → 10 wasted
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            AltTabSwitches = 5,
            AltTabKeystrokes = 15,
        };

        var result = StatsCalculator.ComputeWastedKeystrokes([], today);

        result.Should().Be(10);
    }

    // ── ComputeStaticPct ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeStaticPct_ReturnsZero_WhenNoSwitches()
    {
        var result = StatsCalculator.ComputeStaticPct([], EmptyToday());

        result.Should().Be(0);
    }

    [Fact]
    public void ComputeStaticPct_ComputesCorrectRatio()
    {
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            StaticAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["a.exe"] = new() { Switches = 75 },
            },
            DynamicAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["b.exe"] = new() { Switches = 25 },
            },
        };

        var result = StatsCalculator.ComputeStaticPct([], today);

        result.Should().Be(75);
    }

    // ── FindPersonalBest ──────────────────────────────────────────────────────

    [Fact]
    public void FindPersonalBest_ReturnsNull_WhenNoBuckets()
    {
        var result = StatsCalculator.FindPersonalBest([], EmptyToday());

        result.Should().BeNull();
    }

    [Fact]
    public void FindPersonalBest_ReturnsFastestAcrossAllBuckets()
    {
        var buckets = new[]
        {
            new DailyBucketDocument
            {
                Date = DateTime.Today.AddDays(-1),
                FastestSwitch = new FastestSwitchRecord { DurationMs = 80, AppName = "slow.exe", Letter = "S" },
            },
            new DailyBucketDocument
            {
                Date = DateTime.Today.AddDays(-2),
                FastestSwitch = new FastestSwitchRecord { DurationMs = 40, AppName = "fast.exe", Letter = "F" },
            },
        };

        var result = StatsCalculator.FindPersonalBest(buckets, EmptyToday());

        result!.DurationMs.Should().Be(40);
        result.AppName.Should().Be("fast.exe");
    }

    [Fact]
    public void FindPersonalBest_TodayCanWin()
    {
        var buckets = new[]
        {
            new DailyBucketDocument
            {
                Date = DateTime.Today.AddDays(-1),
                FastestSwitch = new FastestSwitchRecord { DurationMs = 80, AppName = "slow.exe", Letter = "S" },
            },
        };
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            FastestSwitch = new FastestSwitchRecord { DurationMs = 30, AppName = "today.exe", Letter = "T" },
        };

        var result = StatsCalculator.FindPersonalBest(buckets, today);

        result!.DurationMs.Should().Be(30);
        result.AppName.Should().Be("today.exe");
    }

    // ── ComputeAvgLatency ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeAvgLatency_ReturnsDash_WhenNoData()
    {
        var result = StatsCalculator.ComputeAvgLatency(new Dictionary<string, AppAggregateStats>());

        result.Should().Be("—");
    }

    [Fact]
    public void ComputeAvgLatency_ComputesCorrectAverage()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 4, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 200),
        };

        var result = StatsCalculator.ComputeAvgLatency(combined);

        result.Should().Be("50ms");
    }

    // ── BuildPodium ───────────────────────────────────────────────────────────

    [Fact]
    public void BuildPodium_ReturnsTopThreeBySwitch()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 100, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
            ["b.exe"] = new(Switches: 50, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
            ["c.exe"] = new(Switches: 30, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
            ["d.exe"] = new(Switches: 10, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
        };
        var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = "App A", ["b.exe"] = "App B", ["c.exe"] = "App C", ["d.exe"] = "App D",
        };

        var result = StatsCalculator.BuildPodium(combined, FakeRegistryCache(displayNames));

        result.Select(r => r.ProcessName)
            .Should().BeEquivalentTo("a.exe", "b.exe", "c.exe");
    }

    // ── FindFirstStaleShortcut ────────────────────────────────────────────────

    [Fact]
    public void FindFirstStaleShortcut_ReturnsNull_WhenAllAppsUsed()
    {
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            StaticAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["code.exe"] = new() { Switches = 10 },
            },
        };
        List<(string, Key C)> configuredApps = [("code.exe", Key.C)];

        var result = StatsCalculator.FindFirstStaleShortcut([], today, configuredApps, FakeRegistryCache());

        result.Should().BeNull();
    }

    [Fact]
    public void FindFirstStaleShortcut_ReturnsFirstUnusedApp()
    {
        List<(string, Key C)> configuredApps =
        [
            ("used.exe", Key.U),
            ("stale.exe", Key.S),
        ];
        var today = new DailyBucketDocument
        {
            Date = DateTime.Today,
            StaticAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["used.exe"] = new() { Switches = 5 },
            },
        };

        var displayNames = new Dictionary<string, string>
        {
            ["stale.exe"] = "Stale App",
        };

        var result = StatsCalculator.FindFirstStaleShortcut([], today, configuredApps, FakeRegistryCache(displayNames));

        result.Should().NotBeNull()
            .And.BeEquivalentTo(new StaleShortcutInfo("S", "stale.exe", "Stale App"));
    }

    // ── ComputeAvgGlance ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeAvgGlance_ReturnsDash_WhenNoPeeks()
    {
        var result = StatsCalculator.ComputeAvgGlance(new Dictionary<string, AppAggregateStats>());

        result.Should().Be("—");
    }

    [Fact]
    public void ComputeAvgGlance_ReturnsDash_WhenZeroPeekCount()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 5, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
        };

        var result = StatsCalculator.ComputeAvgGlance(combined);

        result.Should().Be("—");
    }

    [Fact]
    public void ComputeAvgGlance_ReturnsFormattedAverage()
    {
        // 2 peeks, 3000ms total → avg 1500ms → 1.5s
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 2, Peeks: 2, TotalPeekTimeMs: 3000, TotalSwitchTimeMs: 0),
        };

        var result = StatsCalculator.ComputeAvgGlance(combined);

        result.Should().Be($"{1.5:F1}s");
    }

    // ── FindMostPeeked ────────────────────────────────────────────────────────

    [Fact]
    public void FindMostPeeked_ReturnsNull_WhenCombinedIsEmpty()
    {
        var result = StatsCalculator.FindMostPeeked(new Dictionary<string, AppAggregateStats>(),
            FakeRegistryCache());

        result.Should().BeNull();
    }

    [Fact]
    public void FindMostPeeked_ReturnsNull_WhenAllPeeksAreZero()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 5, Peeks: 0, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
        };

        var result = StatsCalculator.FindMostPeeked(combined, FakeRegistryCache());

        result.Should().BeNull();
    }

    [Fact]
    public void FindMostPeeked_ReturnsAppWithMostPeeks()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 10, Peeks: 5, TotalPeekTimeMs: 2000, TotalSwitchTimeMs: 0),
            ["b.exe"] = new(Switches: 3, Peeks: 2, TotalPeekTimeMs: 800, TotalSwitchTimeMs: 0),
        };

        var result = StatsCalculator.FindMostPeeked(combined, FakeRegistryCache());

        result!.ProcessName.Should().Be("a.exe");
        result.Peeks.Should().Be(5);
    }

    [Fact]
    public void FindMostPeeked_UsesDisplayName_FromDictionary()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 1, Peeks: 3, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
        };
        var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = "My App",
        };

        var result = StatsCalculator.FindMostPeeked(combined, FakeRegistryCache(displayNames));

        result!.DisplayName.Should().Be("My App");
    }

    [Fact]
    public void FindMostPeeked_FallsBack_ToFileNameWithoutExtension_WhenNotInDict()
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase)
        {
            ["a.exe"] = new(Switches: 1, Peeks: 3, TotalPeekTimeMs: 0, TotalSwitchTimeMs: 0),
        };

        var result = StatsCalculator.FindMostPeeked(combined, FakeRegistryCache());

        result!.DisplayName.Should().Be("a");
    }

    // ── Compute (integration) ─────────────────────────────────────────────────

    [Fact]
    public void Compute_ReturnsDefaultValues_WhenNoData()
    {
        var result = _sut.Compute([], EmptyToday(), []);

        result.LifeGained.Should().Be("—");
        result.MuscleMemoGrade.Should().Be("—");
        result.TeleportStreak.Should().Be(0);
        result.AltTabRelapsePct.Should().Be(0);
        result.Podium.Should().BeEmpty();
        result.FirstStaleShortcut.Should().BeNull();
        result.PersonalBestRecord.Should().BeNull();
        result.AvgLatency.Should().Be("—");
    }

    [Fact]
    public void Compute_OnlyIncludesLast30DaysInRecentMetrics()
    {
        var oldBucket = new DailyBucketDocument
        {
            Date = DateTime.Today.AddDays(-31),
            AltTabSwitches = 1000, // would skew relapse pct if included
            TotalSwitches = 0,
        };
        var recentBucket = new DailyBucketDocument
        {
            Date = DateTime.Today.AddDays(-1),
            TotalSwitches = 100,
            AltTabSwitches = 0,
        };

        var result = _sut.Compute([oldBucket, recentBucket], EmptyToday(), []);

        result.AltTabRelapsePct.Should().Be(0);
    }
}
