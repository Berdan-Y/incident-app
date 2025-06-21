using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Blazor.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GoogleMapsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException("GoogleMaps:ApiKey not configured");
        _httpClient.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
    }

    public async Task<GeocodingResponse?> ValidateAndGetCoordinatesAsync(string address, string zipCode)
    {
        try
        {
            var formattedAddress = $"{address}, {zipCode}".Replace(" ", "+");
            var response = await _httpClient.GetFromJsonAsync<GoogleMapsApiResponse>(
                $"geocode/json?address={formattedAddress}&key={_apiKey}");

            if (response?.Status != "OK" || !response.Results.Any())
            {
                return new GeocodingResponse { IsValid = false };
            }

            var result = response.Results.First();
            return new GeocodingResponse
            {
                IsValid = true,
                Latitude = result.Geometry.Location.Lat,
                Longitude = result.Geometry.Location.Lng,
                FormattedAddress = result.FormattedAddress
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating address: {ex.Message}");
            return new GeocodingResponse { IsValid = false };
        }
    }

    private class GoogleMapsApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public List<GeocodingResult> Results { get; set; } = new();
    }

    private class GeocodingResult
    {
        public string FormattedAddress { get; set; } = string.Empty;
        public Geometry Geometry { get; set; } = new();
    }

    private class Geometry
    {
        public Location Location { get; set; } = new();
    }

    private class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
} 