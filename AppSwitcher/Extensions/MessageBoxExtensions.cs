
using Wpf.Ui.Controls;

namespace AppSwitcher.Extensions;

public static class MessageBoxExtensions
{
    /// <summary>
    /// Sync version of ShowDialogAsync, for use in contexts where async/await is not possible (e.g. application startup).
    /// </summary>
    /// <param name="messageBox"></param>
    public static MessageBoxResult ShowSync(this MessageBox messageBox)
    {
        return messageBox.ShowDialogAsync().GetAwaiter().GetResult();
    }
}