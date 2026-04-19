using AppSwitcher.Configuration;
using System.Windows;
using Wpf.Ui.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace AppSwitcher.UI.Controls;

public partial class CycleModeSelector : UserControl
{
    public static readonly DependencyProperty SelectedCycleModeProperty =
        DependencyProperty.Register(nameof(SelectedCycleMode), typeof(CycleMode), typeof(CycleModeSelector),
            new FrameworkPropertyMetadata(CycleMode.NextApp, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedCycleModeChanged));

    public CycleMode SelectedCycleMode
    {
        get => (CycleMode)GetValue(SelectedCycleModeProperty);
        set => SetValue(SelectedCycleModeProperty, value);
    }

    public CycleModeSelector()
    {
        InitializeComponent();
        UpdateButtonStates();
    }

    private void CycleModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            if (Enum.TryParse<CycleMode>(tag, out var cycleMode))
            {
                SelectedCycleMode = cycleMode;
            }
        }
    }

    private static void OnSelectedCycleModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CycleModeSelector selector)
        {
            selector.UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        var selectedTag = SelectedCycleMode.ToString();

        NextAppButton.Appearance = GetButtonAppearance(NextAppButton.Tag?.ToString());
        HideButton.Appearance = GetButtonAppearance(HideButton.Tag?.ToString());
        NextWindowButton.Appearance = GetButtonAppearance(NextWindowButton.Tag?.ToString());
        return;

        ControlAppearance GetButtonAppearance(string? tag) =>
            tag == selectedTag ? ControlAppearance.Primary : ControlAppearance.Secondary;
    }
}

