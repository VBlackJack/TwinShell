using System.Globalization;
using System.Windows.Data;

namespace TwinShell.App.Converters;

/// <summary>
/// Multi-value converter that returns true if two values are equal (by reference for objects).
/// Used for comparing objects in DataTriggers.
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return false;

        var first = values[0];
        var second = values[1];

        if (first == null && second == null)
            return true;

        if (first == null || second == null)
            return false;

        return ReferenceEquals(first, second) || first.Equals(second);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
