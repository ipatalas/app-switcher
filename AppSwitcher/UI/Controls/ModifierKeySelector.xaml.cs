using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace AppSwitcher.UI.Controls;

public partial class ModifierKeySelector : UserControl
{
    public static readonly DependencyProperty SelectedModifierProperty =
        DependencyProperty.Register(nameof(SelectedModifier), typeof(Key),
            typeof(ModifierKeySelector),
            new PropertyMetadata(Key.None, OnSelectedModifierChanged));

    public Key SelectedModifier
    {
        get => (Key)GetValue(SelectedModifierProperty);
        set => SetValue(SelectedModifierProperty, value);
    }

    public ModifierKeySelector()
    {
        InitializeComponent();
    }

    private void ModifierButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button { Tag: string tag })
        {
            SelectedModifier = Enum.Parse<Key>(tag);
        }
    }

    private static void OnSelectedModifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModifierKeySelector control)
        {
            control.UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        RightAltButton.Appearance = GetButtonAppearance(Key.RightAlt);
        RightCtrlButton.Appearance = GetButtonAppearance(Key.RightCtrl);
        RightShiftButton.Appearance = GetButtonAppearance(Key.RightShift);
        AppsButton.Appearance = GetButtonAppearance(Key.Apps);
        return;

        ControlAppearance GetButtonAppearance(Key key) =>
            SelectedModifier == key ? ControlAppearance.Primary : ControlAppearance.Secondary;
    }
}