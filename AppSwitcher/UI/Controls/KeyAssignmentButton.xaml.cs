using System.Windows;
using System.Windows.Input;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyboardFocusChangedEventArgs = System.Windows.Input.KeyboardFocusChangedEventArgs;

namespace AppSwitcher.UI.Controls;

public partial class KeyAssignmentButton : UserControl
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.Register(
            nameof(Key),
            typeof(Key),
            typeof(KeyAssignmentButton),
            new FrameworkPropertyMetadata(
                Key.None,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnKeyChanged));

    public Key Key
    {
        get => (Key)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public static readonly DependencyProperty ButtonContentProperty =
        DependencyProperty.Register(
            nameof(ButtonContent),
            typeof(string),
            typeof(KeyAssignmentButton),
            new PropertyMetadata("?"));

    public string ButtonContent
    {
        get => (string)GetValue(ButtonContentProperty);
        private set => SetValue(ButtonContentProperty, value);
    }

    public static readonly DependencyProperty ButtonAppearanceProperty =
        DependencyProperty.Register(
            nameof(ButtonAppearance),
            typeof(ControlAppearance),
            typeof(KeyAssignmentButton),
            new PropertyMetadata(ControlAppearance.Secondary));

    public ControlAppearance ButtonAppearance
    {
        get => (ControlAppearance)GetValue(ButtonAppearanceProperty);
        private set => SetValue(ButtonAppearanceProperty, value);
    }

    private static readonly DependencyProperty IsListeningProperty =
        DependencyProperty.Register(
            nameof(IsListening),
            typeof(bool),
            typeof(KeyAssignmentButton),
            new PropertyMetadata(false));

    private bool IsListening
    {
        get => (bool)GetValue(IsListeningProperty);
        set => SetValue(IsListeningProperty, value);
    }

    public KeyAssignmentButton()
    {
        InitializeComponent();
    }

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyAssignmentButton control)
        {
            var newKey = (Key)e.NewValue;
            control.UpdateDisplay(newKey);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (IsListening)
        {
            StopListening();
        }
        else
        {
            StartListening();
        }

        e.Handled = true;
    }

    private void Button_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsListening)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            StopListening();
            e.Handled = true;
            return;
        }

        if (e.Key is >= Key.A and <= Key.Z)
        {
            SetCurrentValue(KeyProperty, e.Key); // Update via binding to propagate to parent
            StopListening();
            e.Handled = true;
            return;
        }

        // Ignore all other keys (stay in listening mode)
        e.Handled = true;
    }

    private void Button_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (IsListening)
        {
            StopListening();
        }
    }

    private void UpdateDisplay(Key key)
    {
        if (IsListening)
        {
            return;
        }

        ButtonContent = key == Key.None ? "?" : key.ToString();
        ButtonAppearance = ControlAppearance.Secondary;
    }

    internal void StartListening()
    {
        IsListening = true;
        ButtonContent = "...";
        ButtonAppearance = ControlAppearance.Primary;
        InnerButton.Focus();
        Keyboard.Focus(InnerButton);
    }

    private void StopListening()
    {
        IsListening = false;
        UpdateDisplay(Key); // Refresh display with current key
    }
}
