using System.Diagnostics;
using System.Globalization;

namespace Maui.Converters;

public class MultiBindingBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"MultiBindingBoolConverter.Convert called with {values.Length} values");

        for (int i = 0; i < values.Length; i++)
        {
            Debug.WriteLine($"Value {i}: {values[i]} (Type: {values[i]?.GetType().Name ?? "null"})");
        }

        // Ensure we have exactly 2 values
        if (values.Length != 2)
        {
            Debug.WriteLine($"MultiBindingBoolConverter: Expected 2 values, got {values.Length}, returning false");
            return false;
        }

        // Get the IsLoggedIn result (first value)
        bool isLoggedIn = values[0] is bool isLoggedInValue && isLoggedInValue;
        Debug.WriteLine($"MultiBindingBoolConverter: IsLoggedIn = {isLoggedIn}");

        // Get the HasRole result (second value)
        bool hasRole = values[1] is bool hasRoleValue && hasRoleValue;
        Debug.WriteLine($"MultiBindingBoolConverter: HasRole = {hasRole}");

        // Both conditions must be true
        var result = isLoggedIn && hasRole;
        Debug.WriteLine($"MultiBindingBoolConverter result: {result}");
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}