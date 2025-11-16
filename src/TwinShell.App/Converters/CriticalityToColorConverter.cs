using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class CriticalityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CriticalityLevel level)
        {
            return level switch
            {
                CriticalityLevel.Info => new SolidColorBrush(Color.FromRgb(33, 150, 243)),      // Blue
                CriticalityLevel.Run => new SolidColorBrush(Color.FromRgb(76, 175, 80)),        // Green
                CriticalityLevel.Dangerous => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
