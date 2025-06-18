using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Maui.Services;

public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GoogleGeocodingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "Google Maps API key not found in configuration");
    }

    public async Task<(bool success, double? latitude, double? longitude, string? errorMessage)> GeocodeAddressAsync(string address, string zipCode)
    {
        try
        {
            var formattedAddress = $"{address}, {zipCode}";
            var encodedAddress = Uri.EscapeDataString(formattedAddress);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                return (false, null, null, $"Geocoding failed: {status}");
            }

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                return (false, null, null, "No results found for the provided address");
            }

            var location = results[0]
                .GetProperty("geometry")
                .GetProperty("location");

            var latitude = location.GetProperty("lat").GetDouble();
            var longitude = location.GetProperty("lng").GetDouble();

            return (true, latitude, longitude, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Geocoding error: {ex.Message}");
        }
    }
} 