using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AppSwitcher.UI.ViewModels;

internal class SettingsViewModelDirtyTracker : IDisposable
{
    private readonly SettingsViewModel _model;
    private readonly Action _onChange;

    public SettingsViewModelDirtyTracker(SettingsViewModel model, Action onChange)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _onChange = onChange ?? throw new ArgumentNullException(nameof(onChange));

        _model.PropertyChanged += Model_PropertyChanged;
        HookCollection(_model.Applications);
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_model.ModifierKey):
            case nameof(_model.PulseBorderEnabled):
            case nameof(_model.ModifierIdleTimeoutMs):
                _onChange();
                break;
            case nameof(_model.Applications):
                // Collection reference changed → resubscribe
                UnhookCollection();
                HookCollection(_model.Applications);
                _onChange();
                break;
        }
    }

    private void Applications_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
            {
                item.PropertyChanged += ApplicationItem_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
            {
                item.PropertyChanged -= ApplicationItem_PropertyChanged;
            }
        }

        _onChange();
    }

    private void ApplicationItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ApplicationShortcutViewModel.ValidationError)
                           or nameof(ApplicationShortcutViewModel.HasValidationError))
        {
            return;
        }

        _onChange();
    }

    private void HookCollection(ObservableCollection<ApplicationShortcutViewModel> collection)
    {
        collection.CollectionChanged += Applications_CollectionChanged;

        foreach (var item in collection)
        {
            item.PropertyChanged += ApplicationItem_PropertyChanged;
        }
    }

    private void UnhookCollection()
    {
        _model.Applications.CollectionChanged -= Applications_CollectionChanged;

        foreach (var item in _model.Applications)
        {
            item.PropertyChanged -= ApplicationItem_PropertyChanged;
        }
    }

    public void Dispose()
    {
        _model.PropertyChanged -= Model_PropertyChanged;
        UnhookCollection();
    }
}