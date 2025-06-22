using System.Globalization;
using System.Diagnostics;
using Maui.Services;
using Shared.Models.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Maui.Converters;

public class IsAssignedFieldEmployeeConverter : IValueConverter
{
    private ITokenService GetTokenService()
    {
        var tokenService = Application.Current?.Resources["TokenService"] as ITokenService;
        return tokenService;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        if (value is not IncidentResponseDto incident)
        {
            return false;
        }

        var tokenService = GetTokenService();
        if (tokenService == null)
        {
            return false;
        }

        var isLoggedIn = tokenService.IsLoggedIn;
        var hasRole = tokenService.HasRole("FieldEmployee");
        var currentUserId = tokenService.GetUserId();
        var assignedToId = incident.AssignedTo?.Id.ToString();

        // Check if user is logged in and has FieldEmployee role
        if (!isLoggedIn || !hasRole)
        {
            return false;
        }

        // Check if the incident is assigned to the current user
        var isAssigned = assignedToId == currentUserId;
        return isAssigned;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}