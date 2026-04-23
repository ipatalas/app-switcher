using AppSwitcher.Configuration;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace AppSwitcher.UI.ViewModels.Common;

internal interface ISettingsState : INotifyPropertyChanged
{
    Key ModifierKey { get; set; }
    bool PulseBorderEnabled { get; set; }
    AppThemeSetting Theme { get; set; }
    bool OverlayEnabled { get; set; }
    int OverlayShowDelayMs { get; set; }
    bool OverlayKeepOpenWhileModifierHeld { get; set; }
    bool PeekEnabled { get; set; }
    bool DynamicModeEnabled { get; set; }
    bool StatsEnabled { get; set; }
    ObservableCollection<ApplicationShortcutViewModel> Applications { get; set; }
    bool HasNoApplications { get; }
    ObservableCollection<DynamicApplicationViewModel> DynamicApplications { get; set; }
    bool HasDynamicApplications { get; }
    bool LaunchAtStartup { get; set; }

    bool IsDirty { get; }
    bool CanSave { get; }
    bool HasValidationErrors { get; }
    string? ValidationSummary { get; }

    void LoadConfiguration();
    bool Save();
    ApplicationShortcutViewModel? AddApplication(ApplicationSelectionArgs args);
    void RemoveApplication(ApplicationShortcutViewModel application);
    ApplicationShortcutViewModel? PinApplication(DynamicApplicationViewModel dynamic);
}