
namespace AppSwitcher.Extensions;

public static class MessageBoxExtensions
{
    /// <summary>
    /// Sync version of ShowDialogAsync, for use in contexts where async/await is not possible (e.g. application startup).
    /// </summary>
    /// <param name="messageBox"></param>
    public static void ShowSync(this Wpf.Ui.Controls.MessageBox messageBox)
    {
        messageBox.ShowDialogAsync().GetAwaiter().GetResult();
    }
}