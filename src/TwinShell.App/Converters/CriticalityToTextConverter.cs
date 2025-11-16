using System.Globalization;
using System.Windows.Data;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class CriticalityToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CriticalityLevel level)
        {
            return level switch
            {
                CriticalityLevel.Info => "Info",
                CriticalityLevel.Run => "Run",
                CriticalityLevel.Dangerous => "Dangerous",
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
