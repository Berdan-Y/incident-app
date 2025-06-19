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

    public async Task<(bool success, string? address, string? zipCode, string? errorMessage)> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                return (false, null, null, $"Reverse geocoding failed: {status}");
            }

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                return (false, null, null, "No results found for the provided coordinates");
            }

            var result = results[0];
            var addressComponents = result.GetProperty("address_components");
            string? streetNumber = null;
            string? route = null;
            string? zipCode = null;

            foreach (var component in addressComponents.EnumerateArray())
            {
                var types = component.GetProperty("types").EnumerateArray();
                var longName = component.GetProperty("long_name").GetString();

                foreach (var type in types)
                {
                    var typeStr = type.GetString();
                    switch (typeStr)
                    {
                        case "street_number":
                            streetNumber = longName;
                            break;
                        case "route":
                            route = longName;
                            break;
                        case "postal_code":
                            zipCode = longName;
                            break;
                    }
                }
            }

            // Combine street number and route for the address
            string? address = null;
            if (!string.IsNullOrEmpty(route))
            {
                address = string.IsNullOrEmpty(streetNumber) ? route : $"{streetNumber} {route}";
            }

            if (string.IsNullOrEmpty(address) && string.IsNullOrEmpty(zipCode))
            {
                return (false, null, null, "Could not extract address and zip code from the response");
            }

            return (true, address, zipCode, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Reverse geocoding error: {ex.Message}");
        }
    }
}