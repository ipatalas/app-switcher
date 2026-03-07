using AppSwitcher.Utils;
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

    public event Action<string, string>? ApplicationSelected;

    [UsedImplicitly(Reason = "Design-time constructor")]
    public AddApplicationFlyoutViewModel()
    {
        var chromeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/chrome.png"));
        var notepadIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/notepad.png"));
        var explorerIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/explorer.png"));

        _filteredApplications = new ObservableCollection<RunningApplicationInfo>([
            new("chrome.exe", "chrome.exe", chromeIcon),
            new("notepad.exe", "notepad.exe", notepadIcon),
            new("explorer.exe", "explorer.exe", explorerIcon)
        ]);
    }

    public AddApplicationFlyoutViewModel(RunningApplicationsService runningApplicationsService)
    {
        _runningApplicationsService = runningApplicationsService;
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
        ApplicationSelected?.Invoke(application.ProcessName, application.ProcessImageName);
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
            ApplicationSelected?.Invoke(Path.GetFileName(dialog.FileName), dialog.FileName);
        }
    }
}
