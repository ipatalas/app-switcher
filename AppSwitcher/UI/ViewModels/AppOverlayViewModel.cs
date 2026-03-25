using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using System.Windows.Media.Imaging;

namespace AppSwitcher.UI.ViewModels;

public record OverlayAppItem(Key HotkeyKey, string Name, ImageSource? Icon);

public partial class AppOverlayViewModel : ObservableObject
{
    public ObservableCollection<OverlayAppItem> RunningApps { get; } = [];
    public ObservableCollection<OverlayAppItem> LaunchableApps { get; } = [];

    public bool HasRunningApps => RunningApps.Count > 0;
    public bool HasLaunchableApps => LaunchableApps.Count > 0;

    [UsedImplicitly(Reason = "Design-time constructor")]
    public AppOverlayViewModel()
    {
        var chromeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/chrome.png"));
        var notepadIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/notepad.png"));
        var explorerIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DesignTime/explorer.png"));

        RunningApps = new ObservableCollection<OverlayAppItem>([
            new(Key.C, "chrome", chromeIcon),
            new(Key.N, "notepad.exe", notepadIcon)
        ]);

        LaunchableApps = new ObservableCollection<OverlayAppItem>([
            new(Key.E, "explorer.exe", explorerIcon)
        ]);
    }

    public void Update(IEnumerable<OverlayAppItem> running, IEnumerable<OverlayAppItem> launchable)
    {
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

        OnPropertyChanged(nameof(HasRunningApps));
        OnPropertyChanged(nameof(HasLaunchableApps));
    }
}