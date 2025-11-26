using System.Globalization;
using System.Windows.Data;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts between nullable Platform and boolean for RadioButton binding.
/// Used for filtering examples by platform where null means "show all".
/// </summary>
public class NullablePlatformToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // If parameter is null, check if value is also null
        if (parameter == null)
        {
            return value == null;
        }

        // If parameter is a Platform enum, check if value equals it
        if (parameter is Platform platformParam)
        {
            if (value is Platform platformValue)
            {
                return platformValue == platformParam;
            }
            return false;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Only convert back if the RadioButton is checked (value is true)
        if (value is bool isChecked && isChecked)
        {
            // If parameter is null, return null (show all)
            if (parameter == null)
            {
                return null;
            }

            // If parameter is a Platform enum, return it
            if (parameter is Platform platform)
            {
                return platform;
            }
        }

        // Return Binding.DoNothing to prevent unchecking from changing the value
        return Binding.DoNothing;
    }
}
