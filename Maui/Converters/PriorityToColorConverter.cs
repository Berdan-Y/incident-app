using System.Globalization;
using Shared.Models.Enums;

namespace Maui.Converters;

public class PriorityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Priority priority)
        {
            return priority switch
            {
                Priority.Low => Colors.Green,
                Priority.Medium => Colors.Orange,
                Priority.High => Colors.Red,
                Priority.Critical => Colors.DarkRed,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}