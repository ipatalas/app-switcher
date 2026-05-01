using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows.Media.Imaging;

namespace AppSwitcher.UI.ViewModels.DesignTime;

internal class StatsSettingsViewModelDesignTime : StatsSettingsViewModel
{
    public StatsSettingsViewModelDesignTime()
        : base(
            new SettingsStateDesignTime(),
            NullLogger<StatsSettingsViewModel>.Instance,
            CreateDesignStats(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!)
    {
        var chromeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/chrome.png"));
        var notepadIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/notepad.png"));
        var codeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/code.png"));
        var explorerIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/explorer.png"));

        LifeGained = "1h 42m";
        TeleportStreak = 12;
        TodaySwitchCount = 13;
        MuscleMemoGrade = "B";
        MuscleMemoPersona = "The Navigator";
        TotalSwitchCount = 2510;
        AvgLatency = "38ms";
        PersonalBestDisplay = "45ms";
        PersonalBestLabel = "Modifier+C → Code";
        PersonalBestIcon = codeIcon;
        PersonalBestTooltip = "Achieved on 10 April 2026";
        TotalPeekCount = 450;
        AvgGlanceSec = "1.2s";
        AltTabRelapsePct = 15;
        AltTabSoberStreak = 3;
        WastedKeystrokes = 162;
        StaticPct = 65;
        IsMaintenanceHealthy = false;

        Podium =
        [
            new AppStatEntry("Code.exe", "VS Code", codeIcon, 1240, 5, 6000),
            new AppStatEntry("Chrome.exe", "Chrome", chromeIcon, 850, 12, 6000),
            new AppStatEntry("notepad.exe", "Notepad", notepadIcon, 420, 3, 2400),
        ];

        // IsSessionWarmup = true;
        // Podium = [];

        FirstStaleShortcut = new StaleShortcutEntry("E", "explorer.exe", "Explorer", explorerIcon);

        MostPeekedApp = new AppStatEntry("WindowsTerminal.exe", "Terminal", notepadIcon, 850, 120, 144000);
    }

    private static SessionStats CreateDesignStats()
    {
        var stats = new SessionStats();
        stats.LoadFrom(new DailyBucketDocument
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            TotalSwitches = 47,
            TotalTimeSavedMs = 2_840_000,
            TotalPeeks = 12,
            AltTabSwitches = 7,
            AltTabKeystrokes = 23,
            FastestSwitch = new FastestSwitchRecord
            {
                DurationMs = 45,
                AppName = "WindowsTerminal.exe",
                Letter = "T",
                Date = new DateTime(2026, 4, 10),
            },
            StaticAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["Code.exe"] = new() { Switches = 18, Peeks = 3, TotalPeekTimeMs = 4500, TotalSwitchTimeMs = 720 },
                ["WindowsTerminal.exe"] = new() { Switches = 14, Peeks = 2, TotalPeekTimeMs = 1000, TotalSwitchTimeMs = 560 },
                ["brave.exe"] = new() { Switches = 8, Peeks = 1, TotalPeekTimeMs = 800, TotalSwitchTimeMs = 320 },
            },
            DynamicAppUsage = new Dictionary<string, AppUsageStats>
            {
                ["Slack.exe"] = new() { Switches = 5, Peeks = 6, TotalPeekTimeMs = 18000, TotalSwitchTimeMs = 200 },
                ["notepad.exe"] = new() { Switches = 2, Peeks = 0, TotalPeekTimeMs = 0, TotalSwitchTimeMs = 80 },
            },
        });
        return stats;
    }
}