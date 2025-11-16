using System.Globalization;
using System.Windows.Data;
using TwinShell.Core.Enums;

namespace TwinShell.App.Converters;

public class PlatformToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Platform platform)
        {
            return platform switch
            {
                Platform.Windows => "🪟",
                Platform.Linux => "🐧",
                Platform.Both => "🪟🐧",
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
