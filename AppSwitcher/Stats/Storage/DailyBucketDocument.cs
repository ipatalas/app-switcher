using LiteDB;

namespace AppSwitcher.Stats.Storage;

internal class FastestSwitchRecord
{
    public int DurationMs { get; init; }
    public string AppName { get; init; } = string.Empty;
    public string Letter { get; init; } = string.Empty;
    public DateTime Date { get; init; }

    public FastestSwitchRecord Clone() =>
        new() { DurationMs = DurationMs, AppName = AppName, Letter = Letter, Date = Date };
}

internal class AppUsageStats
{
    public int Switches { get; set; }
    public int Peeks { get; set; }
    public int TotalPeekTimeMs { get; set; }
    public int TotalSwitchTimeMs { get; set; }

    public AppUsageStats Clone() =>
        new()
        {
            Switches = Switches,
            Peeks = Peeks,
            TotalPeekTimeMs = TotalPeekTimeMs,
            TotalSwitchTimeMs = TotalSwitchTimeMs,
        };
}

internal class DailyBucketDocument
{
    public const string CollectionName = "daily_buckets";

    [BsonId]
    public DateOnly Date { get; init; }

    public int TotalSwitches { get; init; }

    public int TotalTimeSavedMs { get; init; }

    public int TotalPeeks { get; init; }

    public Dictionary<string, AppUsageStats> StaticAppUsage { get; init; } = [];

    public Dictionary<string, AppUsageStats> DynamicAppUsage { get; init; } = [];

    public Dictionary<string, int> Transitions { get; init; } = [];

    public int AltTabSwitches { get; init; }

    public int AltTabKeystrokes { get; init; }

    public FastestSwitchRecord? FastestSwitch { get; init; }
}