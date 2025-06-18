using System.Globalization;
using Shared.Models.Enums;

namespace Maui.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Status status)
        {
            return status switch
            {
                Status.Todo => Colors.Blue,
                Status.InProgress => Colors.Orange,
                Status.Done => Colors.Green,
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