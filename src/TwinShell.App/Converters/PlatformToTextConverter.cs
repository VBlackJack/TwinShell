using System.Globalization;
using System.Windows.Data;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class PlatformToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Platform platform)
        {
            return platform switch
            {
                Platform.Windows => "Windows",
                Platform.Linux => "Linux",
                Platform.Both => "Both",
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
