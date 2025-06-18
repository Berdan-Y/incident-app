using System.Threading.Tasks;

namespace Maui.Services;

public interface IGeocodingService
{
    Task<(bool success, double? latitude, double? longitude, string? errorMessage)> GeocodeAddressAsync(string address, string zipCode);
} 