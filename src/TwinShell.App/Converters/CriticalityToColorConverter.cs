using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class CriticalityToColorConverter : IValueConverter
{
    // PERFORMANCE: Reuse static brush instances instead of creating new ones on each conversion
    private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(33, 150, 243));      // Blue
    private static readonly SolidColorBrush RunBrush = new(Color.FromRgb(76, 175, 80));        // Green
    private static readonly SolidColorBrush DangerousBrush = new(Color.FromRgb(244, 67, 54));  // Red
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Gray);

    static CriticalityToColorConverter()
    {
        // Freeze brushes for better performance
        InfoBrush.Freeze();
        RunBrush.Freeze();
        DangerousBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CriticalityLevel level)
        {
            return level switch
            {
                CriticalityLevel.Info => InfoBrush,
                CriticalityLevel.Run => RunBrush,
                CriticalityLevel.Dangerous => DangerousBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
