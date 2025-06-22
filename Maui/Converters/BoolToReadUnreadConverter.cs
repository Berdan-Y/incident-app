using System.Globalization;

namespace Maui.Converters;

public class BoolToReadUnreadConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRead)
        {
            return isRead ? "Mark as Unread" : "Mark as Read";
        }
        return "Mark as Read";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}