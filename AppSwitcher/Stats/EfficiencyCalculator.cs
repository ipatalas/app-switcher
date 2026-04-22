namespace AppSwitcher.Stats;

internal static class EfficiencyCalculator
{
    private const int AltTabBaseOverhead = 500;
    private const int AltTabScanConstant = 150;
    private const int AltTabPhysicalTaps = 400;
    private const int AppSwitcherExecution = 350;

    /// <summary>
    /// Estimated time (ms) a user would spend using Alt+Tab to switch to one of <paramref name="windowCount"/> apps.
    /// T_at = base + round(scan × log₂(n+1)) + physical_taps
    /// </summary>
    public static int AltTabTimeMs(int windowCount)
        => AltTabBaseOverhead + (int)Math.Round(AltTabScanConstant * Math.Log2(windowCount + 1)) + AltTabPhysicalTaps;

    /// <summary>
    /// Milliseconds saved by using AppSwitcher instead of Alt+Tab. Never negative.
    /// </summary>
    public static int SavedMs(int windowCount)
        => Math.Max(0, AltTabTimeMs(windowCount) - AppSwitcherExecution);
}