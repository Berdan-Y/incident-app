using System.Globalization;
using Maui.Services;
using Shared.Models.Dtos;

namespace Maui.Converters;

public class IsCurrentUserConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tokenService = Application.Current?.Handler?.MauiContext?.Services.GetService<ITokenService>();
        if (tokenService == null) return false;

        var currentUserId = tokenService.GetUserId();
        if (string.IsNullOrEmpty(currentUserId)) return false;

        // Handle different input types
        string creatorId = null;
        if (value is string userId)
        {
            creatorId = userId;
        }
        else if (value is UserDto user)
        {
            creatorId = user.Id;
        }

        // If we couldn't get a creator ID, this is likely an anonymous report
        if (string.IsNullOrEmpty(creatorId))
        {
            return false;
        }

        return currentUserId == creatorId;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}