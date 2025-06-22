using System.Diagnostics;
using System.Globalization;

namespace Maui.Converters;

public class MultiBindingBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Ensure we have exactly 2 values
        if (values.Length != 2)
        {
            return false;
        }

        // Get the IsLoggedIn result (first value)
        bool isLoggedIn = values[0] is bool isLoggedInValue && isLoggedInValue;

        // Get the HasRole result (second value)
        bool hasRole = values[1] is bool hasRoleValue && hasRoleValue;

        // Both conditions must be true
        var result = isLoggedIn && hasRole;
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}