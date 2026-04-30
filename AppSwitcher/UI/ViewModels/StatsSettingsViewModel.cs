using AppSwitcher.Extensions;
using AppSwitcher.Stats;
using AppSwitcher.Stats.Storage;
using AppSwitcher.UI.ViewModels.Common;
using AppSwitcher.WindowDiscovery;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace AppSwitcher.UI.ViewModels;

internal partial class StatsSettingsViewModel(
    ISettingsState state,
    ILogger<StatsSettingsViewModel> logger,
    SessionStats sessionStats,
    StatsRepository statsRepository,
    AppRegistryCache appRegistryCache,
    IconExtractor iconExtractor,
    IContentDialogService dialogService,
    StatsDbProvider statsDbProvider,
    StatsCalculator statsCalculator)
    : ObservableObject
{
    public ISettingsState State { get; } = state;

    // ── Hero metrics ──────────────────────────────────────────────────────────

    [ObservableProperty] private string _lifeGained = "—";
    [ObservableProperty] private int _teleportStreak;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TodaySwitchProgress))]
    [NotifyPropertyChangedFor(nameof(IsTodayStreakComplete))]
    private int _todaySwitchCount;

    public int TodaySwitchProgress => Math.Min(TodaySwitchCount, 20) * 5;
    public bool IsTodayStreakComplete => TodaySwitchCount >= 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MuscleMemoIconBrush))]
    private string _muscleMemoGrade = "—";

    [ObservableProperty] private string _muscleMemoPersona = "No data yet";

    public Brush MuscleMemoIconBrush => GradeToIconBrush(MuscleMemoGrade);

    private static Brush GradeToIconBrush(string grade) => grade switch
    {
        "S" => new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)), // Electric purple — Ascended
        "A" => (Brush)Application.Current.FindResource("SystemAccentColorBrush")!,
        "B" => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)), // Green — solid
        "C" or "D" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)), // Amber — room for improvement
        "F" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), // Red — habit alert
        _ => Brushes.Gold,   // Gold — no data
    };

    // ── Most Used Apps ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPodiumData))]
    private IReadOnlyList<AppStatEntry> _podium = [];

    public bool HasPodiumData => Podium.Count > 0 && !IsSessionWarmup;

    // ── Maintenance ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMaintenanceWarning))]
    [NotifyPropertyChangedFor(nameof(IsMaintenanceActuallyHealthy))]
    private bool _isMaintenanceHealthy;

    public bool IsMaintenanceWarning => !IsMaintenanceHealthy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMaintenanceActuallyHealthy))]
    private bool _isMaintenanceWarmup;

    public bool IsMaintenanceActuallyHealthy => IsMaintenanceHealthy && !IsMaintenanceWarmup;

    [ObservableProperty] private StaleShortcutEntry? _firstStaleShortcut;

    // ── Habit Tracker ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AltTabCuredPct))]
    private int _altTabRelapsePct;

    public int AltTabCuredPct => 100 - AltTabRelapsePct;

    [ObservableProperty] private int _altTabSoberStreak;
    [ObservableProperty] private int _wastedKeystrokes;

    // ── Speed Demon ───────────────────────────────────────────────────────────

    [ObservableProperty] private int _totalSwitchCount;
    [ObservableProperty] private string _avgLatency = "—";
    [ObservableProperty] private string _personalBestDisplay = "—";
    [ObservableProperty] private string _personalBestLabel = "—";
    [ObservableProperty] private string? _personalBestTooltip;
    [ObservableProperty] private ImageSource? _personalBestIcon;

    // ── Peek Story ────────────────────────────────────────────────────────────

    [ObservableProperty] private int _totalPeekCount;
    [ObservableProperty] private string _avgGlanceSec = "—";
    [ObservableProperty] private AppStatEntry? _mostPeekedApp;

    // ── Discovery Gap ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicPct))]
    private int _staticPct;

    public int DynamicPct => 100 - StaticPct;

    // ── Loading state ─────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isLoading;

    // ── Warmup state ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDataReady))]
    [NotifyPropertyChangedFor(nameof(HasPodiumData))]
    private bool _isSessionWarmup;

    public bool IsDataReady => !IsSessionWarmup;

    // ── Historic buckets cache (window lifetime) ──────────────────────────────

    private readonly object _historicBucketsCacheLock = new();
    private IReadOnlyList<DailyBucketDocument>? _historicBucketsCache;

    private IReadOnlyList<DailyBucketDocument> GetHistoricBuckets()
    {
        if (_historicBucketsCache is not null)
        {
            return _historicBucketsCache;
        }

        if (!State.StatsEnabled)
        {
            return [];
        }

        lock (_historicBucketsCacheLock)
        {
            _historicBucketsCache ??= statsRepository.GetAllHistoricBuckets();
            return _historicBucketsCache;
        }
    }
    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task EnableStats()
    {
        State.StatsEnabled = true;
        State.Save();
        await RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task DisableStats()
    {
        State.StatsEnabled = false;
        State.Save();
        await RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ResetStats()
    {
        var dialog = new ContentDialog
        {
            Title = "Reset all stats?",
            Content = "This action cannot be undone.",
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Reset",
            PrimaryButtonAppearance = ControlAppearance.Danger,
            DefaultButton = ContentDialogButton.Close,
        };
        var result = await dialogService.ShowAsync(dialog, CancellationToken.None);

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                statsDbProvider.Delete();
                sessionStats.Clear();
                await RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to reset stats");
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while resetting stats:\n{e.Message}\n\nPlease try again.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                };
                await dialogService.ShowAsync(errorDialog, CancellationToken.None);
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            var data = await Task.Run(ComputeMetrics);
            ApplyMetrics(data);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemoveStaleShortcut(StaleShortcutEntry entry)
    {
        var app = State.Applications.FirstOrDefault(a =>
            a.ProcessName.Equals(entry.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (app is null)
        {
            return;
        }

        State.RemoveApplication(app);
        State.Save();

        await RefreshCommand.ExecuteAsync(null);
    }

    // ── Computation ───────────────────────────────────────────────────────────

    private StatsMetrics ComputeMetrics()
    {
        using var _ = logger.MeasureTime($"ComputeStatsMetrics(IsCached = {_historicBucketsCache is not null})");

        var today = sessionStats.Snapshot(DateTime.Today);
        var allBuckets = GetHistoricBuckets();

        var configuredApps = State.Applications
            .Select(a => (a.ProcessName, a.Key))
            .ToList();

        var metrics = statsCalculator.Compute(allBuckets, today, configuredApps);

        return metrics;
    }

    private void ApplyMetrics(StatsMetrics d)
    {
        LifeGained = d.LifeGained;
        TeleportStreak = d.TeleportStreak;
        TodaySwitchCount = d.TodaySwitchCount;
        MuscleMemoGrade = d.MuscleMemoGrade;
        MuscleMemoPersona = d.MuscleMemoPersona;
        AltTabRelapsePct = d.AltTabRelapsePct;
        AltTabSoberStreak = d.AltTabSoberStreak;
        WastedKeystrokes = d.WastedKeystrokes;
        TotalSwitchCount = d.TotalSwitchCount;
        TotalPeekCount = d.TotalPeekCount;
        AvgGlanceSec = d.AvgGlanceSec;
        AvgLatency = d.AvgLatency;
        StaticPct = d.StaticPct;

        Podium = d.Podium
            .Select(e => new AppStatEntry(e.ProcessName, e.DisplayName, ResolveIcon(e.ProcessName), e.Switches, e.Peeks, e.TotalPeekTimeMs))
            .ToList();

        MostPeekedApp = d.MostPeekedApp is { } peeked
            ? new AppStatEntry(peeked.ProcessName, peeked.DisplayName, ResolveIcon(peeked.ProcessName), peeked.Switches, peeked.Peeks, peeked.TotalPeekTimeMs)
            : null;

        if (d.FirstStaleShortcut is { } stale)
        {
            FirstStaleShortcut = new StaleShortcutEntry(stale.Letter, stale.ProcessName, stale.DisplayName, ResolveIcon(stale.ProcessName));
            IsMaintenanceHealthy = false;
        }
        else
        {
            FirstStaleShortcut = null;
            IsMaintenanceHealthy = true;
        }

        IsMaintenanceWarmup = d.IsMaintenanceWarmup;
        IsSessionWarmup = !State.StatsEnabled || d.IsSessionWarmup;

        if (d.PersonalBestRecord is { } best)
        {
            PersonalBestDisplay = $"{best.DurationMs}ms";
            var name = appRegistryCache.GetDisplayName(best.AppName);
            PersonalBestLabel = $"Modifier+{best.Letter} → {name}";
            PersonalBestIcon = ResolveIcon(best.AppName);
            PersonalBestTooltip = best.Date != default
                ? $"Achieved on {best.Date:d MMMM yyyy}"
                : null;
        }
        else
        {
            PersonalBestDisplay = "—";
            PersonalBestLabel = "—";
            PersonalBestIcon = null;
            PersonalBestTooltip = null;
        }
    }

    // ── Icon resolution ───────────────────────────────────────────────────────

    private ImageSource? ResolveIcon(string processName)
    {
        var staticMatch = State.Applications.FirstOrDefault(a =>
            a.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

        if (staticMatch?.ProcessIcon is not null)
        {
            return staticMatch.ProcessIcon;
        }

        var dynamicMatch = State.DynamicApplications.FirstOrDefault(a =>
            a.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

        if (dynamicMatch?.ProcessIcon is not null)
        {
            return dynamicMatch.ProcessIcon;
        }

        return iconExtractor.GetDefaultIcon();
    }
}