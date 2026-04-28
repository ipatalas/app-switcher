using AppSwitcher.Stats.Storage;
using System.Collections.Immutable;
using System.IO;
using System.Windows.Input;

namespace AppSwitcher.Stats;

internal record StatsMetrics(
    string LifeGained,
    int TeleportStreak,
    string MuscleMemoGrade,
    string MuscleMemoPersona,
    IReadOnlyList<AppUsageSummary> Podium,
    AppUsageSummary? MostPeekedApp,
    StaleShortcutInfo? FirstStaleShortcut,
    bool IsMaintenanceWarmup,
    bool IsSessionWarmup,
    int AltTabRelapsePct,
    int AltTabSoberStreak,
    int WastedKeystrokes,
    int TotalSwitchCount,
    FastestSwitchRecord? PersonalBestRecord,
    int TotalPeekCount,
    string AvgGlanceSec,
    string AvgLatency,
    int StaticPct);

/// <summary>Pure app usage data without any WPF/UI types — safe to use in tests.</summary>
internal record AppUsageSummary(
    string ProcessName,
    string DisplayName,
    int Switches,
    int Peeks,
    int TotalPeekTimeMs);

/// <summary>Stale shortcut data without any WPF/UI types — safe to use in tests.</summary>
internal record StaleShortcutInfo(
    string Letter,
    string ProcessName,
    string DisplayName);

internal record AppAggregateStats(int Switches, int Peeks, int TotalPeekTimeMs, int TotalSwitchTimeMs);

internal record MuscleMemoResult(string Grade, string Persona);

internal class StatsCalculator(AppRegistryCache appRegistryCache)
{
    public StatsMetrics Compute(IReadOnlyList<DailyBucketDocument> allBuckets,
        DailyBucketDocument today,
        List<(string ProcessName, Key Key)> configuredStaticApps)
    {
        var cutoff = today.Date.AddDays(-30);
        var recentBuckets = allBuckets.Where(b => b.Date >= cutoff).ToList();

        var combined = BuildCombinedAppStats(recentBuckets, today);
        bool isMaintenanceWarmup = allBuckets.Count < 14;

        var totalSwitchCount = recentBuckets.Sum(b => b.TotalSwitches) + today.TotalSwitches;
        bool isSessionWarmup = totalSwitchCount < 10;

        var muscleMemo = (isSessionWarmup ? null : ComputeMuscleMemoGrade(recentBuckets, today)) ??
                         new MuscleMemoResult("—", "Measuring...");

        return new StatsMetrics(
            LifeGained: ComputeLifeGained(allBuckets, today),
            TeleportStreak: ComputeStreak(allBuckets, today.Date, b => b.TotalSwitches >= 20),
            MuscleMemoGrade: muscleMemo.Grade,
            MuscleMemoPersona: muscleMemo.Persona,
            Podium: BuildPodium(combined, appRegistryCache),
            MostPeekedApp: FindMostPeeked(combined, appRegistryCache),
            FirstStaleShortcut: isMaintenanceWarmup ? null : FindFirstStaleShortcut(recentBuckets, today, configuredStaticApps, appRegistryCache),
            IsMaintenanceWarmup: isMaintenanceWarmup,
            IsSessionWarmup: isSessionWarmup,
            AltTabRelapsePct: ComputeRelapsePct(recentBuckets, today),
            AltTabSoberStreak: ComputeStreak(allBuckets, today.Date, IsSoberDay),
            WastedKeystrokes: ComputeWastedKeystrokes(recentBuckets, today),
            TotalSwitchCount: totalSwitchCount,
            PersonalBestRecord: isSessionWarmup ? null : FindPersonalBest(allBuckets, today),
            TotalPeekCount: recentBuckets.Sum(b => b.TotalPeeks) + today.TotalPeeks,
            AvgGlanceSec: ComputeAvgGlance(combined),
            AvgLatency: ComputeAvgLatency(isSessionWarmup ? ImmutableDictionary<string, AppAggregateStats>.Empty : combined),
            StaticPct: ComputeStaticPct(recentBuckets, today));
    }

    // ── Computation helpers (internal for unit testing) ───────────────────────

    internal static string ComputeLifeGained(
        IReadOnlyList<DailyBucketDocument> allBuckets,
        DailyBucketDocument today)
    {
        var totalMs = allBuckets.Sum(b => (long)b.TotalTimeSavedMs) + today.TotalTimeSavedMs;
        if (totalMs <= 0)
        {
            return "—";
        }

        var t = TimeSpan.FromMilliseconds(totalMs);

        if (t.TotalSeconds < 60)
        {
            return $"{(int)t.TotalSeconds}s";
        }

        if (t.TotalMinutes < 60)
        {
            return t.Seconds > 0 ? $"{(int)t.TotalMinutes}m {t.Seconds}s" : $"{(int)t.TotalMinutes}m";
        }

        return $"{(int)t.TotalHours}h {t.Minutes}m";
    }

    internal static MuscleMemoResult? ComputeMuscleMemoGrade(
        IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today)
    {
        var staticSwitches = recent.Sum(b => b.StaticAppUsage.Values.Sum(v => v.Switches))
                           + today.StaticAppUsage.Values.Sum(v => v.Switches);
        var dynamicSwitches = recent.Sum(b => b.DynamicAppUsage.Values.Sum(v => v.Switches))
                            + today.DynamicAppUsage.Values.Sum(v => v.Switches);
        var relapses = recent.Sum(b => b.AltTabSwitches) + today.AltTabSwitches;
        var total = staticSwitches + dynamicSwitches + relapses;

        if (total == 0)
        {
            return null;
        }

        var index = (1.0 * staticSwitches + 0.7 * dynamicSwitches) / total * 100;

        return index switch
        {
            >= 96 => new MuscleMemoResult("S", "Shadow Walker"),
            >= 85 => new MuscleMemoResult("A", "Teleporter"),
            >= 70 => new MuscleMemoResult("B", "The Navigator"),
            >= 50 => new MuscleMemoResult("C", "Learner"),
            >= 30 => new MuscleMemoResult("D", "Novice"),
            _ => new MuscleMemoResult("F", "Alt-Tabber"),
        };
    }

    internal static int ComputeStreak(
        IReadOnlyList<DailyBucketDocument> allBuckets,
        DateTime today,
        Func<DailyBucketDocument, bool> predicate)
    {
        var streak = 0;
        var checkDate = today.AddDays(-1);

        foreach (var bucket in allBuckets)
        {
            if (bucket.Date != checkDate || !predicate(bucket))
            {
                break;
            }

            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return streak;
    }

    internal static int ComputeRelapsePct(
        IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today)
    {
        var altTabTotal = recent.Sum(b => b.AltTabSwitches) + today.AltTabSwitches;
        var switchTotal = recent.Sum(b => b.TotalSwitches) + today.TotalSwitches;
        var grandTotal = altTabTotal + switchTotal;

        return grandTotal == 0 ? 0 : (int)Math.Round(altTabTotal * 100.0 / grandTotal);
    }

    internal static bool IsSoberDay(DailyBucketDocument bucket)
    {
        var total = bucket.TotalSwitches + bucket.AltTabSwitches;
        return total == 0 || bucket.TotalSwitches * 100.0 / total > 95;
    }

    internal static int ComputeWastedKeystrokes(
        IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today)
    {
        return recent.Sum(b => Math.Max(0, b.AltTabKeystrokes - b.AltTabSwitches))
             + Math.Max(0, today.AltTabKeystrokes - today.AltTabSwitches);
    }

    internal static int ComputeStaticPct(
        IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today)
    {
        var staticTotal = recent.Sum(b => b.StaticAppUsage.Values.Sum(v => v.Switches))
                        + today.StaticAppUsage.Values.Sum(v => v.Switches);
        var dynamicTotal = recent.Sum(b => b.DynamicAppUsage.Values.Sum(v => v.Switches))
                         + today.DynamicAppUsage.Values.Sum(v => v.Switches);
        var grandTotal = staticTotal + dynamicTotal;

        return grandTotal == 0 ? 0 : (int)Math.Round(staticTotal * 100.0 / grandTotal);
    }

    internal static FastestSwitchRecord? FindPersonalBest(
        IReadOnlyList<DailyBucketDocument> allBuckets,
        DailyBucketDocument today)
    {
        var candidates = allBuckets
            .Where(b => b.FastestSwitch is not null)
            .Select(b => b.FastestSwitch!);

        if (today.FastestSwitch is not null)
        {
            candidates = candidates.Append(today.FastestSwitch);
        }

        return candidates.MinBy(f => f.DurationMs);
    }

    internal static string ComputeAvgGlance(IReadOnlyDictionary<string, AppAggregateStats> combined)
    {
        var totalPeekMs = combined.Values.Sum(v => (long)v.TotalPeekTimeMs);
        var totalPeeks = combined.Values.Sum(v => v.Peeks);

        if (totalPeeks == 0)
        {
            return "—";
        }

        return $"{totalPeekMs / (totalPeeks * 1000.0):F1}s";
    }

    internal static string ComputeAvgLatency(IReadOnlyDictionary<string, AppAggregateStats> combined)
    {
        var totalSwitchTimeMs = combined.Values.Sum(v => (long)v.TotalSwitchTimeMs);
        var totalSwitches = combined.Values.Sum(v => v.Switches);

        if (totalSwitches == 0 || totalSwitchTimeMs == 0)
        {
            return "—";
        }

        return $"{totalSwitchTimeMs / totalSwitches}ms";
    }

    internal static IReadOnlyList<AppUsageSummary> BuildPodium(IReadOnlyDictionary<string, AppAggregateStats> combined,
        AppRegistryCache appRegistryCache)
    {
        return combined
            .OrderByDescending(kvp => kvp.Value.Switches)
            .Take(3)
            .Select(kvp => new AppUsageSummary(
                kvp.Key,
                appRegistryCache.GetDisplayName(kvp.Key),
                kvp.Value.Switches,
                kvp.Value.Peeks,
                kvp.Value.TotalPeekTimeMs))
            .ToList();
    }

    internal static AppUsageSummary? FindMostPeeked(
        IReadOnlyDictionary<string, AppAggregateStats> combined,
        AppRegistryCache appRegistryCache)
    {
        var top = combined
            .Where(kvp => kvp.Value.Peeks > 0)
            .OrderByDescending(kvp => kvp.Value.Peeks)
            .FirstOrDefault();

        if (top.Key is null)
        {
            return null;
        }

        return new AppUsageSummary(
            top.Key,
            appRegistryCache.GetDisplayName(top.Key),
            top.Value.Switches,
            top.Value.Peeks,
            top.Value.TotalPeekTimeMs);
    }

    internal static StaleShortcutInfo? FindFirstStaleShortcut(IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today,
        List<(string ProcessName, Key Key)> configuredStaticApps,
        AppRegistryCache appRegistryCache)
    {
        foreach (var (processName, letter) in configuredStaticApps)
        {
            var totalSwitches = recent.Sum(b =>
                b.StaticAppUsage.TryGetValue(processName, out var s) ? s.Switches : 0)
                + (today.StaticAppUsage.TryGetValue(processName, out var ts) ? ts.Switches : 0);

            if (totalSwitches == 0)
            {
                return new StaleShortcutInfo(
                    letter.ToString(),
                    processName,
                    appRegistryCache.GetDisplayName(processName));
            }
        }

        return null;
    }

    private static Dictionary<string, AppAggregateStats> BuildCombinedAppStats(
        IReadOnlyList<DailyBucketDocument> recent,
        DailyBucketDocument today)
    {
        var combined = new Dictionary<string, AppAggregateStats>(StringComparer.OrdinalIgnoreCase);

        foreach (var bucket in recent.Append(today))
        {
            Accumulate(combined, bucket.StaticAppUsage);
            Accumulate(combined, bucket.DynamicAppUsage);
        }

        return combined;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void Accumulate(
        Dictionary<string, AppAggregateStats> combined,
        IReadOnlyDictionary<string, AppUsageStats> usage)
    {
        foreach (var (key, stats) in usage)
        {
            if (combined.TryGetValue(key, out var existing))
            {
                combined[key] = new AppAggregateStats(
                    Switches: existing.Switches + stats.Switches,
                    Peeks: existing.Peeks + stats.Peeks,
                    TotalPeekTimeMs: existing.TotalPeekTimeMs + stats.TotalPeekTimeMs,
                    TotalSwitchTimeMs: existing.TotalSwitchTimeMs + stats.TotalSwitchTimeMs);
            }
            else
            {
                combined[key] = new AppAggregateStats(
                    stats.Switches,
                    stats.Peeks,
                    stats.TotalPeekTimeMs,
                    stats.TotalSwitchTimeMs);
            }
        }
    }
}