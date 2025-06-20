using System.Globalization;

namespace Maui.Converters;

public class OrBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2 || !values.All(v => v is bool))
            return false;

        return (bool)values[0] || (bool)values[1];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 