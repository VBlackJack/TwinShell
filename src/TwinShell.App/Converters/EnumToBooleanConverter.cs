using System.Globalization;
using System.Windows.Data;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts between an enum value and a boolean for RadioButton binding.
/// The parameter should be the enum value name as a string.
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // Get the enum value name from the parameter
        var parameterString = parameter.ToString();
        if (string.IsNullOrEmpty(parameterString))
            return false;

        // Compare the enum value with the parameter
        return value.ToString() == parameterString;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null || !(value is bool boolValue) || !boolValue)
            return Binding.DoNothing;

        // Parse the parameter as an enum value
        var parameterString = parameter.ToString();
        if (string.IsNullOrEmpty(parameterString))
            return Binding.DoNothing;

        try
        {
            return Enum.Parse(targetType, parameterString);
        }
        catch
        {
            return Binding.DoNothing;
        }
    }
}
