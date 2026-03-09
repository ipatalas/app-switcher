using AppSwitcher.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class General : Page
{
    public General(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ModifierIdleTimeout_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e is { Source: NumberBox numberBox, Text: "\r" })
        {
            if (int.TryParse(numberBox.Text, out var number))
            {
                ((SettingsViewModel)DataContext).ModifierIdleTimeoutMs = number;
            }
            else if (string.IsNullOrEmpty(numberBox.Text))
            {
                ((SettingsViewModel)DataContext).ModifierIdleTimeoutMs = null;
            }
            e.Handled = true;
        }
    }
}