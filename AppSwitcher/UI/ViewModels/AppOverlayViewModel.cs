using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using System.Windows.Media.Imaging;

namespace AppSwitcher.UI.ViewModels;

public record OverlayAppItem(Key HotkeyKey, string Name, ImageSource? Icon, bool IsActive = false, bool IsElevated = false);

public partial class AppOverlayViewModel : ObservableObject
{
    public ObservableCollection<OverlayAppItem> FocusedAppWindows { get; } = [];
    public ObservableCollection<OverlayAppItem> RunningApps { get; } = [];
    public ObservableCollection<OverlayAppItem> LaunchableApps { get; } = [];

    public bool ShowFocusedAppWindows => FocusedAppWindows.Count > 1;
    public bool ShowRunningApps => RunningApps.Count > 0;
    public bool ShowLaunchableApps => LaunchableApps.Count > 0;

    [ObservableProperty]
    private string? _focusedAppName;

    [UsedImplicitly(Reason = "Design-time constructor")]
    public AppOverlayViewModel()
    {
        var chromeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/chrome.png"));
        var notepadIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/notepad.png"));
        var explorerIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/explorer.png"));
        var codeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/code.png"));

        FocusedAppWindows = new ObservableCollection<OverlayAppItem>([
            new(Key.D1, "Welcome - Visual Studio Code", codeIcon, IsActive: true),
            new(Key.D2, "README.md - Visual Studio Code", codeIcon)
        ]);

        FocusedAppName = "code";

        RunningApps = new ObservableCollection<OverlayAppItem>([
            new(Key.C, "chrome", chromeIcon, IsElevated: true),
            new(Key.N, "notepad", notepadIcon)
        ]);

        LaunchableApps = new ObservableCollection<OverlayAppItem>([
            new(Key.E, "explorer", explorerIcon)
        ]);
    }

    public void Update(
        IEnumerable<OverlayAppItem> focusedWindows,
        string? focusedAppName,
        IEnumerable<OverlayAppItem> running,
        IEnumerable<OverlayAppItem> launchable)
    {
        FocusedAppWindows.Clear();
        foreach (var item in focusedWindows)
        {
            FocusedAppWindows.Add(item);
        }

        RunningApps.Clear();
        foreach (var item in running)
        {
            RunningApps.Add(item);
        }

        LaunchableApps.Clear();
        foreach (var item in launchable)
        {
            LaunchableApps.Add(item);
        }

        FocusedAppName = focusedAppName;

        OnPropertyChanged(nameof(ShowFocusedAppWindows));
        OnPropertyChanged(nameof(ShowRunningApps));
        OnPropertyChanged(nameof(ShowLaunchableApps));
    }
}