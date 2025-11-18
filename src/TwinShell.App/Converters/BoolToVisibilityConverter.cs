using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TwinShell.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Check if we should invert the result
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

            if (invert)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
