using System.Windows;
using System.Windows.Media;

namespace AppSwitcher.Extensions;

public static class DependencyObjectExtensions
{
    public static T? FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var result = child.FindVisualChild<T>();
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}