using AppSwitcher.Extensions;
using LiteDB;
using System.Windows.Input;

namespace AppSwitcher.Stats.Storage;

internal class FastestSwitchRecord
{
    public int DurationMs { get; init; }
    public required string AppName { get; init; }
    public required string Letter { get; init; }
    public DateTime Date { get; init; }

    public FastestSwitchRecord Clone() => new()
    {
        DurationMs = DurationMs,
        AppName = AppName,
        Letter = Letter,
        Date = Date,
    };
}

internal class AppUsageStats(int switches = 0, int peeks = 0, int totalPeekTimeMs = 0, int totalSwitchTimeMs = 0)
{
    public int Switches { get; private set; } = switches;
    public int Peeks { get; private set; } = peeks;
    public int TotalPeekTimeMs { get; private set; } = totalPeekTimeMs;
    public int TotalSwitchTimeMs { get; private set; } = totalSwitchTimeMs;

    public static AppUsageStats NewSwitch(int durationMs) => new() { Switches = 1, TotalSwitchTimeMs = durationMs, };

    public static AppUsageStats NewPeek(int durationMs) => new() { Peeks = 1, TotalPeekTimeMs = durationMs, };

    public void AddSwitch(int durationMs)
    {
        Switches++;
        TotalSwitchTimeMs += durationMs;
    }

    public void AddPeek(int durationMs)
    {
        Peeks++;
        TotalPeekTimeMs += durationMs;
    }

    public AppUsageStats Clone() => new()
    {
        Switches = Switches, Peeks = Peeks, TotalPeekTimeMs = TotalPeekTimeMs, TotalSwitchTimeMs = TotalSwitchTimeMs,
    };
}

internal class DailyBucketDocument
{
    public const string CollectionName = "daily_buckets";

    [BsonId]
    public DateOnly Date { get; set; }

    public int TotalSwitches { get; set; }

    public int TotalTimeSavedMs { get; set; }

    public int TotalPeeks { get; set; }

    public Dictionary<string, AppUsageStats> StaticAppUsage { get; init; } = [];

    public Dictionary<string, AppUsageStats> DynamicAppUsage { get; init; } = [];

    public int AltTabSwitches { get; set; }

    public int AltTabKeystrokes { get; set; }

    public FastestSwitchRecord? FastestSwitch { get; set; }

    public void RecordSwitch(string processName, int durationMs, int savedMs, bool isDynamic)
    {
        TotalSwitches++;
        TotalTimeSavedMs += savedMs;

        var bucket = isDynamic ? DynamicAppUsage : StaticAppUsage;
        if (bucket.TryGetValue(processName, out var existingStats))
        {
            existingStats.AddSwitch(durationMs);
        }
        else
        {
            bucket[processName] = AppUsageStats.NewSwitch(durationMs);
        }
    }

    public void RecordPeek(string processName, int durationMs, bool isDynamic)
    {
        TotalPeeks++;

        var bucket = isDynamic ? DynamicAppUsage : StaticAppUsage;
        if (bucket.TryGetValue(processName, out var existingStats))
        {
            existingStats.AddPeek(durationMs);
        }
        else
        {
            bucket[processName] = AppUsageStats.NewPeek(durationMs);
        }
    }

    public void RecordFastestSwitch(string processName, int? fastestDurationMs, Key triggerKey)
    {
        if (fastestDurationMs is > 0 && (FastestSwitch is null ||
                                         fastestDurationMs.Value < FastestSwitch.DurationMs))
        {
            FastestSwitch = new FastestSwitchRecord
            {
                DurationMs = fastestDurationMs.Value,
                AppName = processName,
                Letter = triggerKey.ToFriendlyString(),
                Date = DateTime.Today,
            };
        }
    }

    public void RecordAltTab(int tabCount)
    {
        AltTabSwitches++;
        AltTabKeystrokes += tabCount;
    }

    public DailyBucketDocument Clone() =>
        new()
        {
            Date = Date,
            TotalSwitches = TotalSwitches,
            TotalTimeSavedMs = TotalTimeSavedMs,
            TotalPeeks = TotalPeeks,
            StaticAppUsage = StaticAppUsage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
            DynamicAppUsage = DynamicAppUsage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
            AltTabSwitches = AltTabSwitches,
            AltTabKeystrokes = AltTabKeystrokes,
            FastestSwitch = FastestSwitch?.Clone()
        };
}