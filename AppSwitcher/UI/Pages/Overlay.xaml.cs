using AppSwitcher.UI.ViewModels;
using AppSwitcher.UI.ViewModels.Common;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace AppSwitcher.UI.Pages;

internal partial class Overlay : Page
{
    public Overlay(OverlaySettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel.State;
    }

    private void OverlayShowDelay_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e is { Source: NumberBox numberBox, Text: "\r" })
        {
            if (int.TryParse(numberBox.Text, out var number))
            {
                ((ISettingsState)DataContext).OverlayShowDelayMs = number;
            }
            e.Handled = true;
        }
    }
}
