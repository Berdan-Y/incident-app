namespace Blazor.Services;

public interface IGoogleMapsService
{
    Task<GeocodingResponse?> ValidateAndGetCoordinatesAsync(string address, string zipCode);
}

public class GeocodingResponse
{
    public bool IsValid { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? FormattedAddress { get; set; }
} 