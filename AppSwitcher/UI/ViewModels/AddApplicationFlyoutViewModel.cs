using AppSwitcher.Configuration;
using AppSwitcher.Extensions;
using AppSwitcher.WindowDiscovery;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AppSwitcher.UI.ViewModels;

internal partial class AddApplicationFlyoutViewModel : ObservableObject
{
    private IReadOnlyList<RunningApplicationInfo> _allApplications = [];

    [ObservableProperty]
    private ObservableCollection<RunningApplicationInfo> _filteredApplications = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    private readonly RunningApplicationsService _runningApplicationsService = null!;
    private readonly ProcessInspector _processInspector = null!;

    public event Action<ApplicationSelectionArgs>? ApplicationSelected;

    [UsedImplicitly(Reason = "Design-time constructor")]
    public AddApplicationFlyoutViewModel()
    {
        var chromeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/chrome.png"));
        var notepadIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/notepad.png"));
        var explorerIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/explorer.png"));

        _filteredApplications = new ObservableCollection<RunningApplicationInfo>([
            new(0, "chrome.exe", "chrome.exe", chromeIcon, false),
            new(0, "notepad.exe", "notepad.exe", notepadIcon, false),
            new(0, "explorer.exe", "explorer.exe", explorerIcon, false)
        ]);
    }

    public AddApplicationFlyoutViewModel(RunningApplicationsService runningApplicationsService, ProcessInspector processInspector)
    {
        _runningApplicationsService = runningApplicationsService;
        _processInspector = processInspector;
    }

    public void Refresh(IEnumerable<string> excludedProcessNames)
    {
        SearchText = string.Empty;

        _allApplications = _runningApplicationsService.GetRunningApplications(excludedProcessNames);
        FilteredApplications = new ObservableCollection<RunningApplicationInfo>(_allApplications);
    }

    partial void OnSearchTextChanged(string value)
    {
        var filtered = string.IsNullOrWhiteSpace(value)
            ? _allApplications
            : _allApplications.Where(a => a.ProcessName.Contains(value, StringComparison.OrdinalIgnoreCase));

        FilteredApplications = new ObservableCollection<RunningApplicationInfo>(filtered);
    }

    [RelayCommand]
    private void SelectApplication(RunningApplicationInfo application)
    {
        ApplicationSelected?.Invoke(new ApplicationSelectionArgs(
            ProcessName: application.ProcessName,
            ProcessPath: application.ProcessImagePath,
            ProcessId: application.ProcessId,
            Type: application.IsPackagedApp ? ApplicationType.Packaged : ApplicationType.Win32));
    }

    [RelayCommand]
    private void BrowseForFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select application",
            Filter = "Executable files (*.exe)|*.exe",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            if (_processInspector.IsWindowsExecutable(dialog.FileName))
            {
                ApplicationSelected?.Invoke(new ApplicationSelectionArgs(
                    ProcessName: Path.GetFileName(dialog.FileName),
                    ProcessPath: dialog.FileName,
                    ProcessId: null,
                    Type: ApplicationType.Win32));
            }
            else
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Invalid application",
                    Content = "The selected file is not a valid Windows executable.",
                    CloseButtonIcon = new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Controls.SymbolRegular.ErrorCircle24),
                    CloseButtonText = "OK",
                }.ShowSync();
            }
        }
    }
}