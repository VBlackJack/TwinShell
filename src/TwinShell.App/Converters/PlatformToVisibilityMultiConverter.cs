using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

/// <summary>
/// Converts Platform enum to Visibility based on whether Windows or Linux command tabs should be visible
/// </summary>
public class PlatformToVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not Platform platform)
            return Visibility.Collapsed;

        // Windows tab: visible for Windows or Both
        // Linux tab: visible for Linux or Both
        // We'll determine based on the tab header context in the XAML
        // For now, just show if platform is Windows, Linux, or Both
        return platform switch
        {
            Platform.Windows => Visibility.Visible,
            Platform.Linux => Visibility.Visible,
            Platform.Both => Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
