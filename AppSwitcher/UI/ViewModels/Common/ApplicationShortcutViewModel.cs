using AppSwitcher.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace AppSwitcher.UI.ViewModels.Common;

internal partial class ApplicationShortcutViewModel : ObservableObject, IApplicationConfiguration
{
    [ObservableProperty] private Key _key;

    [ObservableProperty] private string _processName = null!;
    [ObservableProperty] private string _processPath = null!;
    [ObservableProperty] private string _displayName = null!;
    [ObservableProperty] private bool _startIfNotRunning;
    [ObservableProperty] private CycleMode _cycleMode = CycleMode.NextWindow;
    [ObservableProperty] private ApplicationType _type = ApplicationType.Win32;
    [ObservableProperty] private string? _aumid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private string? _validationError;

    public bool HasValidationError => ValidationError is not null;

    public ImageSource? ProcessIcon { get; init; }

    public void AddError(string error)
    {
        ValidationError = ValidationError is null ? error : $"• {ValidationError}{Environment.NewLine}• {error}".Trim();

        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(ValidationError));
    }

    public void ClearErrors()
    {
        ValidationError = null;

        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(ValidationError));
    }
}