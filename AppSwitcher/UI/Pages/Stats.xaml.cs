using AppSwitcher.Stats;
using AppSwitcher.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AppSwitcher.UI.Pages;

internal partial class Stats : Page
{
    private readonly DispatcherTimer _debounceTimer;

    public Stats(StatsSettingsViewModel viewModel, SessionStats sessionStats, ILogger<Stats> logger)
    {
        InitializeComponent();
        DataContext = viewModel;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += async (_, _) =>
        {
            _debounceTimer.Stop();
            try
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing stats");
            }
        };

        Loaded += async (_, _) =>
        {
            sessionStats.DataChanged += OnSessionDataChanged;
            try
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading stats");
            }
        };

        Unloaded += (_, _) =>
        {
            sessionStats.DataChanged -= OnSessionDataChanged;
            _debounceTimer.Stop();
        };
    }

    private void OnSessionDataChanged()
    {
        Dispatcher.InvokeAsync(() =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        });
    }
}