using LiteDB;

namespace AppSwitcher.Stats.Storage;

internal class AppUsageStats
{
    public int Switches { get; set; }
    public int Peeks { get; set; }
    public int TotalPeekTimeMs { get; set; }

    public AppUsageStats Clone() =>
        new() { Switches = Switches, Peeks = Peeks, TotalPeekTimeMs = TotalPeekTimeMs };
}

internal class DailyBucketDocument
{
    [BsonId]
    public DateTime Date { get; init; }

    public int TotalSwitches { get; init; }

    public int TotalTimeSavedMs { get; init; }

    public int TotalPeeks { get; init; }

    public Dictionary<string, AppUsageStats> StaticAppUsage { get; init; } = [];

    public Dictionary<string, AppUsageStats> DynamicAppUsage { get; init; } = [];

    public Dictionary<string, int> Transitions { get; init; } = [];
}