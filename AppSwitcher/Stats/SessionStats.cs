using AppSwitcher.Stats.Storage;
using System.Collections.Concurrent;

namespace AppSwitcher.Stats;

internal class SessionStats
{
    private int _totalSwitches;
    private int _totalTimeSavedMs;
    private int _totalPeeks;
    private int _altTabSwitches;
    private int _altTabKeystrokes;

    private FastestSwitchRecord? _fastestSwitch;
    private readonly object _fastestSwitchLock = new();

    private readonly ConcurrentDictionary<string, AppUsageStats> _staticAppUsage =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, AppUsageStats> _dynamicAppUsage =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _transitions =
        new(StringComparer.OrdinalIgnoreCase);

    public void RecordSwitch(string processName, string? previousProcessName, int savedMs, bool isDynamic,
        int? fastestDurationMs = null, string letter = "A")
    {
        Interlocked.Increment(ref _totalSwitches);
        Interlocked.Add(ref _totalTimeSavedMs, savedMs);

        var bucket = isDynamic ? _dynamicAppUsage : _staticAppUsage;
        bucket.AddOrUpdate(
            processName,
            _ => new AppUsageStats { Switches = 1 },
            (_, existing) =>
            {
                existing.Switches++;
                return existing;
            });

        if (previousProcessName is not null)
        {
            var key = $"{previousProcessName}|{processName}";
            _transitions.AddOrUpdate(key, 1, (_, count) => count + 1);
        }

        if (fastestDurationMs.HasValue)
        {
            lock (_fastestSwitchLock)
            {
                if (_fastestSwitch is null || fastestDurationMs.Value < _fastestSwitch.DurationMs)
                {
                    _fastestSwitch = new FastestSwitchRecord
                    {
                        DurationMs = fastestDurationMs.Value,
                        AppName = processName,
                        Letter = letter
                    };
                }
            }
        }
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
    }

    public void RecordAltTab(int tabCount)
    {
        Interlocked.Increment(ref _altTabSwitches);
        Interlocked.Add(ref _altTabKeystrokes, tabCount);
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

        _transitions.Clear();
        foreach (var (key, value) in doc.Transitions)
        {
            _transitions[key] = value;
        }
    }

    public DailyBucketDocument Snapshot(DateTime date)
    {
        FastestSwitchRecord? fastestSwitchSnapshot;
        lock (_fastestSwitchLock)
        {
            fastestSwitchSnapshot = _fastestSwitch?.Clone();
        }

        return new DailyBucketDocument
        {
            Date = date.Date,
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
                StringComparer.OrdinalIgnoreCase),
            Transitions = new Dictionary<string, int>(_transitions, StringComparer.OrdinalIgnoreCase)
        };
    }
}