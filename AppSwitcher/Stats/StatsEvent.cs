namespace AppSwitcher.Stats;

internal abstract record StatsEvent;

internal sealed record SwitchEvent(
    string ProcessName,
    uint? ProcessId,
    string ProcessPath,
    int TotalChoices,
    long ModifierDownTick,
    long LetterDownTick,
    long? PreviousLetterUpTick,
    bool IsDynamic) : StatsEvent;

internal sealed record PeekEvent(
    string TargetProcessName,
    long ArmTick,
    long FinishTick,
    bool IsDynamic) : StatsEvent;
