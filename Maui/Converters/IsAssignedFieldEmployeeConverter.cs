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
        Debug.WriteLine($"GetTokenService called, TokenService is {(tokenService == null ? "null" : "not null")}");
        return tokenService;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"IsAssignedFieldEmployeeConverter.Convert called with value: {value}");

        if (value is not IncidentResponseDto incident)
        {
            Debug.WriteLine("IsAssignedFieldEmployeeConverter: value is not IncidentResponseDto, returning false");
            return false;
        }

        var tokenService = GetTokenService();
        if (tokenService == null)
        {
            Debug.WriteLine("IsAssignedFieldEmployeeConverter: TokenService is null, returning false");
            return false;
        }

        var isLoggedIn = tokenService.IsLoggedIn;
        var hasRole = tokenService.HasRole("FieldEmployee");
        var currentUserId = tokenService.GetUserId();
        var assignedToId = incident.AssignedTo?.Id.ToString();

        Debug.WriteLine($"IsAssignedFieldEmployeeConverter: IsLoggedIn={isLoggedIn}");
        Debug.WriteLine($"IsAssignedFieldEmployeeConverter: HasFieldEmployeeRole={hasRole}");
        Debug.WriteLine($"IsAssignedFieldEmployeeConverter: CurrentUserId={currentUserId}");
        Debug.WriteLine($"IsAssignedFieldEmployeeConverter: AssignedToId={assignedToId}");

        // Check if user is logged in and has FieldEmployee role
        if (!isLoggedIn || !hasRole)
        {
            Debug.WriteLine("IsAssignedFieldEmployeeConverter: User is not logged in or not a FieldEmployee, returning false");
            return false;
        }

        // Check if the incident is assigned to the current user
        var isAssigned = assignedToId == currentUserId;
        Debug.WriteLine($"IsAssignedFieldEmployeeConverter: Is incident assigned to current user? {isAssigned}");
        return isAssigned;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 