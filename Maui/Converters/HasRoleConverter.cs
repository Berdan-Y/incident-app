using System.Globalization;
using System.Diagnostics;
using Maui.Services;

namespace Maui.Converters;

public class HasRoleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"HasRoleConverter.Convert called with value: {value}, parameter: {parameter}");
        
        if (value == null)
        {
            Debug.WriteLine("HasRoleConverter: value is null, returning false");
            return false;
        }

        if (parameter == null)
        {
            Debug.WriteLine("HasRoleConverter: parameter is null, returning false");
            return false;
        }

        if (value is not ITokenService tokenService)
        {
            Debug.WriteLine($"HasRoleConverter: value is not ITokenService, it's a {value.GetType()}, returning false");
            return false;
        }

        // Check if user is logged in
        if (!tokenService.IsLoggedIn)
        {
            Debug.WriteLine("HasRoleConverter: User is not logged in, returning false");
            return false;
        }

        var requiredRole = parameter.ToString();
        if (string.IsNullOrEmpty(requiredRole))
        {
            Debug.WriteLine("HasRoleConverter: Required role is empty, returning false");
            return false;
        }

        var hasRole = tokenService.HasRole(requiredRole);
        Debug.WriteLine($"HasRoleConverter: Has required role '{requiredRole}'? {hasRole}");
        return hasRole;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 