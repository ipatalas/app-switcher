using AppSwitcher.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AppSwitcher.UI.ViewModels;

internal partial class AddApplicationFlyoutViewModel(RunningApplicationsService runningApplicationsService)
    : ObservableObject
{
    private IReadOnlyList<RunningApplicationInfo> _allApplications = [];

    [ObservableProperty]
    private ObservableCollection<RunningApplicationInfo> _filteredApplications = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    public event Action<string>? ApplicationSelected;

    public void Refresh(IEnumerable<string> excludedProcessNames)
    {
        SearchText = string.Empty;
        _allApplications = runningApplicationsService.GetRunningApplications(excludedProcessNames);
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
        ApplicationSelected?.Invoke(application.ProcessName);
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
            ApplicationSelected?.Invoke(dialog.FileName);
        }
    }
}
