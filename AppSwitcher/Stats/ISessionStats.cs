using System.Windows.Input;

namespace AppSwitcher.Stats;

internal interface ISessionStats
{
    void RecordSwitch(string processName, string? previousProcessName, int durationMs, int savedMs,
        bool isDynamic, int? fastestDurationMs = null, Key triggerKey = Key.A);
    void RecordPeek(string processName, int durationMs, bool isDynamic);
    void RecordAltTab(int tabCount);
}