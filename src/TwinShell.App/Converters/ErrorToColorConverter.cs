using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts boolean IsError to color brush (theme-aware)
/// </summary>
public class ErrorToColorConverter : IValueConverter
{
    // Fallback brushes in case theme resources are not available
    private static readonly SolidColorBrush FallbackErrorBrush = new(Colors.Red);
    private static readonly SolidColorBrush FallbackDefaultBrush = new(Colors.LightGray);

    static ErrorToColorConverter()
    {
        FallbackErrorBrush.Freeze();
        FallbackDefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isError)
        {
            return isError
                ? GetThemeBrush("DangerBrush", FallbackErrorBrush)
                : GetThemeBrush("TextSecondaryBrush", FallbackDefaultBrush);
        }

        return GetThemeBrush("TextSecondaryBrush", FallbackDefaultBrush);
    }

    /// <summary>
    /// Gets a brush from the current theme resources, with fallback.
    /// </summary>
    private static Brush GetThemeBrush(string resourceKey, Brush fallback)
    {
        if (Application.Current?.Resources[resourceKey] is Brush brush)
        {
            return brush;
        }
        return fallback;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
