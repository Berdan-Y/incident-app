using System.Globalization;
using System.Diagnostics;
using Maui.Services;

namespace Maui.Converters;

public class HasRoleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return false;
        }

        if (parameter == null)
        {
            return false;
        }

        if (value is not ITokenService tokenService)
        {
            return false;
        }

        // Check if user is logged in
        if (!tokenService.IsLoggedIn)
        {
            return false;
        }

        var requiredRoles = parameter.ToString()?.Split(',').Select(r => r.Trim());
        if (requiredRoles == null || !requiredRoles.Any())
        {
            return false;
        }


        // Check if user has any of the required roles
        var hasAnyRole = requiredRoles.Any(role => tokenService.HasRole(role));
        return hasAnyRole;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}