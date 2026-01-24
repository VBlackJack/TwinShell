using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class CriticalityToColorConverter : IValueConverter
{
    // Fallback brushes in case theme resources are not available
    private static readonly SolidColorBrush FallbackInfoBrush = new(Color.FromRgb(33, 150, 243));
    private static readonly SolidColorBrush FallbackRunBrush = new(Color.FromRgb(76, 175, 80));
    private static readonly SolidColorBrush FallbackDangerousBrush = new(Color.FromRgb(244, 67, 54));
    private static readonly SolidColorBrush FallbackDefaultBrush = new(Colors.Gray);

    static CriticalityToColorConverter()
    {
        FallbackInfoBrush.Freeze();
        FallbackRunBrush.Freeze();
        FallbackDangerousBrush.Freeze();
        FallbackDefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CriticalityLevel level)
        {
            return level switch
            {
                CriticalityLevel.Info => GetThemeBrush("InfoBrush", FallbackInfoBrush),
                CriticalityLevel.Run => GetThemeBrush("SuccessBrush", FallbackRunBrush),
                CriticalityLevel.Dangerous => GetThemeBrush("DangerBrush", FallbackDangerousBrush),
                _ => GetThemeBrush("TextSecondaryBrush", FallbackDefaultBrush)
            };
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
