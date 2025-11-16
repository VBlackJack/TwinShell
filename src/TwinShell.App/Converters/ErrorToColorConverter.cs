using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts boolean IsError to color brush
/// </summary>
public class ErrorToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isError)
        {
            return isError
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.LightGray);
        }

        return new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
