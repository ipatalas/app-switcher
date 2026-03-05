using AppSwitcher.UI.ViewModels;
using AppSwitcher.Utils;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace AppSwitcher.UI.Controls;

internal partial class AddApplicationFlyout : UserControl
{
    public AddApplicationFlyout()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        ResultsList.SelectedIndex = -1;
        SearchBox.Focus();
    }

    private AddApplicationFlyoutViewModel? ViewModel => DataContext as AddApplicationFlyoutViewModel;

    public event Action? CloseRequested;

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                CloseRequested?.Invoke();
                e.Handled = true;
                break;

            case Key.Down:
                MoveListSelection(1);
                e.Handled = true;
                break;

            case Key.Up:
                MoveListSelection(-1);
                e.Handled = true;
                break;

            case Key.Enter:
                CommitSelection();
                e.Handled = true;
                break;
        }
    }

    private void ResultsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitSelection();
                e.Handled = true;
                break;

            case Key.Escape:
                SearchBox.Focus();
                e.Handled = true;
                break;

            default:
                // Any printable character refocuses the search box so the user can keep typing
                if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Tab)
                {
                    SearchBox.Focus();
                }
                break;
        }
    }

    private void ResultsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CommitSelection();
    }

    private void MoveListSelection(int delta)
    {
        var count = ResultsList.Items.Count;
        if (count == 0)
        {
            return;
        }

        var next = ResultsList.SelectedIndex + delta;

        // Clamp: don't wrap, stay at ends
        next = Math.Max(0, Math.Min(count - 1, next));

        ResultsList.SelectedIndex = next;
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
    }

    private void CommitSelection()
    {
        if (ResultsList.SelectedItem is RunningApplicationInfo selected)
        {
            ViewModel?.SelectApplicationCommand.Execute(selected);
        }
    }
}
