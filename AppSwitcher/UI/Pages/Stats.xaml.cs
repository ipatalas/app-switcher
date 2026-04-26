using AppSwitcher.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class Stats : Page
{
    public Stats(StatsSettingsViewModel viewModel, ILogger<Stats> logger)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            try
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading stats");
            }
        };
    }
}