using AppSwitcher.Configuration;
using System.Globalization;
using System.Windows.Data;

namespace AppSwitcher.UI.Converters;

[ValueConversion(typeof(CycleMode), typeof(string))]
public sealed class CycleModeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CycleMode mode
            ? mode switch
            {
                CycleMode.NextApp => "Next App",
                CycleMode.Hide => "Hide",
                CycleMode.NextWindow => "Next Window",
                _ => value.ToString() ?? string.Empty
            }
            : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}