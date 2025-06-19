using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;

namespace Maui.Services;

public interface IDirectionsService
{
    Task<List<Location>> GetRoutePointsAsync(Location start, Location end);
    Task<Location> GetCurrentLocationAsync();
}

public class GoogleDirectionsService : IDirectionsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IGeolocation _geolocation;

    public GoogleDirectionsService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IGeolocation geolocation)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["GoogleMaps:ApiKey"];
        _geolocation = geolocation;
    }

    public async Task<List<Location>> GetRoutePointsAsync(Location start, Location end)
    {
        try
        {
            var url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                     $"origin={start.Latitude},{start.Longitude}&" +
                     $"destination={end.Latitude},{end.Longitude}&" +
                     $"key={_apiKey}";

            var response = await _httpClient.GetStringAsync(url);
            var routeData = JsonSerializer.Deserialize<DirectionsResponse>(response);

            if (routeData?.Routes == null || !routeData.Routes.Any())
                return new List<Location>();

            var points = DecodePolyline(routeData.Routes[0].Overview_polyline.Points);
            return points;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting directions: {ex}");
            return new List<Location>();
        }
    }

    public async Task<Location> GetCurrentLocationAsync()
    {
        try
        {
            var location = await _geolocation.GetLastKnownLocationAsync();
            if (location == null)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                location = await _geolocation.GetLocationAsync(request);
            }

            return new Location(location.Latitude, location.Longitude);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting current location: {ex}");
            throw;
        }
    }

    private List<Location> DecodePolyline(string encodedPoints)
    {
        var points = new List<Location>();
        var index = 0;
        var len = encodedPoints.Length;
        var lat = 0;
        var lng = 0;

        while (index < len)
        {
            int b, shift = 0, result = 0;
            do
            {
                b = encodedPoints[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            lat += dlat;

            shift = 0;
            result = 0;
            do
            {
                b = encodedPoints[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            lng += dlng;

            points.Add(new Location(lat * 1e-5, lng * 1e-5));
        }

        return points;
    }
}

// Response models for JSON deserialization
public class DirectionsResponse
{
    public List<Route> Routes { get; set; }
}

public class Route
{
    public OverviewPolyline Overview_polyline { get; set; }
}

public class OverviewPolyline
{
    public string Points { get; set; }
}