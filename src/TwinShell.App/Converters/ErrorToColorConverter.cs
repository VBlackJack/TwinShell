using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts boolean IsError to color brush
/// </summary>
public class ErrorToColorConverter : IValueConverter
{
    // PERFORMANCE: Reuse static brush instances instead of creating new ones on each conversion
    private static readonly SolidColorBrush ErrorBrush = new(Colors.Red);
    private static readonly SolidColorBrush DefaultBrush = new(Colors.LightGray);

    static ErrorToColorConverter()
    {
        // Freeze brushes for better performance
        ErrorBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isError)
        {
            return isError ? ErrorBrush : DefaultBrush;
        }

        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
