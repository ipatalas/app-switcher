using AppSwitcher.Extensions;
using AppSwitcher.Stats.Storage;
using System.Windows.Input;

namespace AppSwitcher.Stats;

internal class SessionStats : ISessionStats
{
    private DailyBucketDocument? _rolloverSnapshot;
    private DailyBucketDocument _currentSnapshot = new();
    // All Record* calls happen from one thread, but they can still clash with another thread calling GetSnapshots
    // This could potentially lead to an exception when modifying the dictionary while it's being read, so we use a lock to be safe
    private readonly object _lock = new();

    public event Action? DataChanged;

    public void RecordSwitch(string processName, string? previousProcessName, int durationMs, int savedMs,
        bool isDynamic,
        int? fastestDurationMs = null, Key triggerKey = Key.A)
    {
        lock (_lock)
        {
            CheckRollover();
            _currentSnapshot.RecordSwitch(processName, durationMs, savedMs, isDynamic);
            _currentSnapshot.RecordFastestSwitch(processName, fastestDurationMs, triggerKey);
        }

        DataChanged?.Invoke();
    }

    public void RecordPeek(string processName, int durationMs, bool isDynamic)
    {
        lock (_lock)
        {
            CheckRollover();
            _currentSnapshot.RecordPeek(processName, durationMs, isDynamic);
        }

        DataChanged?.Invoke();
    }

    public void RecordAltTab(int tabCount)
    {
        lock (_lock)
        {
            CheckRollover();
            _currentSnapshot.RecordAltTab(tabCount);
        }

        DataChanged?.Invoke();
    }

    public void LoadFrom(DailyBucketDocument doc)
    {
        lock (_lock)
        {
            _currentSnapshot = doc.Clone();
            _rolloverSnapshot = null;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentSnapshot = new DailyBucketDocument { Date = DateOnly.FromDateTime(DateTime.Today) };
            _rolloverSnapshot = null;
        }
    }

    public void ClearRollover()
    {
        lock (_lock)
        {
            _rolloverSnapshot = null;
        }
    }

    /// <summary>
    /// Returns all buffered snapshots, this could be today's and yesterday's if there was anything recorded yesterday
    /// </summary>
    public IReadOnlyList<DailyBucketDocument> GetSnapshots()
    {
        lock (_lock)
        {
            var today = _currentSnapshot.Clone();
            var previous = _rolloverSnapshot;

            return previous is null ? [today] : [previous, today];
        }
    }

    private void CheckRollover()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today == _currentSnapshot.Date)
        {
            return;
        }

        if (HasAnyData())
        {
            _rolloverSnapshot = _currentSnapshot.Clone();
        }

        _currentSnapshot = new DailyBucketDocument { Date = DateOnly.FromDateTime(DateTime.Today) };
    }

    private bool HasAnyData() =>
        _currentSnapshot.TotalSwitches > 0 || _currentSnapshot.TotalPeeks > 0 || _currentSnapshot.AltTabSwitches > 0;

    /// <summary>
    /// For testing only. Simulates the app having last recorded an event on a prior date,
    /// so that the next <c>Record*</c> call triggers day-rollover detection.
    /// </summary>
    internal void SimulateLastRecordedDate(DateOnly date) => _currentSnapshot.Date = date;
}