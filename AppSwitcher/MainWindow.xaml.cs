using Microsoft.Extensions.Logging;
using System.Windows;

namespace AppSwitcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _logger = logger;
    }

    protected override void OnActivated(EventArgs e)
    {
        _logger.LogDebug("MainWindow activated");
    }

    protected override void OnClosed(EventArgs e)
    {
        _logger.LogDebug("MainWindow closed");
    }
}