using System.Globalization;

namespace Maui.Converters;

public class AccountTabTitleConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values?.Length > 0 && values[0] is bool isLoggedIn)
        {
            return isLoggedIn ? "Logout" : "Login";
        }
        return "Account";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}