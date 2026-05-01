using AppSwitcher.Extensions;
using AppSwitcher.Stats.Storage;
using System.Collections.Concurrent;
using System.Windows.Input;

namespace AppSwitcher.Stats;

internal class SessionStats : ISessionStats
{
    private int _totalSwitches;
    private int _totalTimeSavedMs;
    private int _totalPeeks;
    private int _altTabSwitches;
    private int _altTabKeystrokes;

    private FastestSwitchRecord? _fastestSwitch;
    private readonly object _fastestSwitchLock = new();

    public event Action? DataChanged;

    private readonly ConcurrentDictionary<string, AppUsageStats> _staticAppUsage =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, AppUsageStats> _dynamicAppUsage =
        new(StringComparer.OrdinalIgnoreCase);

    public void RecordSwitch(string processName, string? previousProcessName, int durationMs, int savedMs,
        bool isDynamic,
        int? fastestDurationMs = null, Key triggerKey = Key.A)
    {
        Interlocked.Increment(ref _totalSwitches);
        Interlocked.Add(ref _totalTimeSavedMs, savedMs);

        var bucket = isDynamic ? _dynamicAppUsage : _staticAppUsage;
        bucket.AddOrUpdate(
            processName,
            _ => new AppUsageStats { Switches = 1, TotalSwitchTimeMs = durationMs },
            (_, existing) =>
            {
                existing.Switches++;
                existing.TotalSwitchTimeMs += durationMs;
                return existing;
            });

        if (fastestDurationMs is > 0)
        {
            lock (_fastestSwitchLock)
            {
                if (_fastestSwitch is null || fastestDurationMs.Value < _fastestSwitch.DurationMs)
                {
                    _fastestSwitch = new FastestSwitchRecord
                    {
                        DurationMs = fastestDurationMs.Value,
                        AppName = processName,
                        Letter = triggerKey.ToFriendlyString(),
                        Date = DateTime.Today,
                    };
                }
            }
        }

        DataChanged?.Invoke();
    }

    public void RecordPeek(string processName, int durationMs, bool isDynamic)    {
        Interlocked.Increment(ref _totalPeeks);

        var bucket = isDynamic ? _dynamicAppUsage : _staticAppUsage;
        bucket.AddOrUpdate(
            processName,
            _ => new AppUsageStats { Peeks = 1, TotalPeekTimeMs = durationMs },
            (_, existing) =>
            {
                existing.Peeks++;
                existing.TotalPeekTimeMs += durationMs;
                return existing;
            });

        DataChanged?.Invoke();
    }

    public void RecordAltTab(int tabCount)
    {
        Interlocked.Increment(ref _altTabSwitches);
        Interlocked.Add(ref _altTabKeystrokes, tabCount);
        DataChanged?.Invoke();
    }

    public void LoadFrom(DailyBucketDocument doc)
    {
        _totalSwitches = doc.TotalSwitches;
        _totalTimeSavedMs = doc.TotalTimeSavedMs;
        _totalPeeks = doc.TotalPeeks;
        _altTabSwitches = doc.AltTabSwitches;
        _altTabKeystrokes = doc.AltTabKeystrokes;

        lock (_fastestSwitchLock)
        {
            _fastestSwitch = doc.FastestSwitch?.Clone();
        }

        _staticAppUsage.Clear();
        foreach (var (key, value) in doc.StaticAppUsage)
        {
            _staticAppUsage[key] = value.Clone();
        }

        _dynamicAppUsage.Clear();
        foreach (var (key, value) in doc.DynamicAppUsage)
        {
            _dynamicAppUsage[key] = value.Clone();
        }

    }

    public void Clear()
    {
        _totalSwitches = 0;
        _totalTimeSavedMs = 0;
        _totalPeeks = 0;
        _altTabSwitches = 0;
        _altTabKeystrokes = 0;
        _staticAppUsage.Clear();
        _dynamicAppUsage.Clear();
        _fastestSwitch = null;
    }

    public DailyBucketDocument Snapshot(DateOnly date)
    {
        FastestSwitchRecord? fastestSwitchSnapshot;
        lock (_fastestSwitchLock)
        {
            fastestSwitchSnapshot = _fastestSwitch?.Clone();
        }

        return new DailyBucketDocument
        {
            Date = date,
            TotalSwitches = _totalSwitches,
            TotalTimeSavedMs = _totalTimeSavedMs,
            TotalPeeks = _totalPeeks,
            AltTabSwitches = _altTabSwitches,
            AltTabKeystrokes = _altTabKeystrokes,
            FastestSwitch = fastestSwitchSnapshot,
            StaticAppUsage = _staticAppUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone(),
                StringComparer.OrdinalIgnoreCase),
            DynamicAppUsage = _dynamicAppUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone(),
                StringComparer.OrdinalIgnoreCase)
        };
    }
}