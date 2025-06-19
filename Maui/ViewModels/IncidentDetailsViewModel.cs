using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace Maui.ViewModels;

[QueryProperty(nameof(IncidentId), "id")]
public class IncidentDetailsViewModel : INotifyPropertyChanged
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;
    private readonly IDirectionsService _directionsService;
    private IncidentResponseDto? _incident;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _incidentId = string.Empty;
    private ObservableCollection<Pin> _mapPins = new();
    private bool _hasValidCoordinates;
    private Location _currentLocation;
    private Distance _circleRadius = Distance.FromMeters(50);
    private Location _circleLocation;
    private ObservableCollection<Polyline> _routeLines = new();
    private MapSpan _mapSpan;
    private Polyline _routeLine;
    private ObservableCollection<Location> _geopath = new();

    public Location CurrentLocation
    {
        get => _currentLocation;
        set
        {
            _currentLocation = value;
            OnPropertyChanged();
        }
    }

    public Location CircleLocation
    {
        get => _circleLocation;
        set
        {
            System.Diagnostics.Debug.WriteLine($"Setting CircleLocation to: {value?.Latitude}, {value?.Longitude}");
            _circleLocation = value;
            OnPropertyChanged();
        }
    }

    public Distance CircleRadius
    {
        get => _circleRadius;
        set
        {
            System.Diagnostics.Debug.WriteLine($"Setting CircleRadius to: {value.Meters} meters");
            _circleRadius = value;
            OnPropertyChanged();
        }
    }

    public IncidentResponseDto? Incident
    {
        get => _incident;
        set
        {
            _incident = value;
            OnPropertyChanged();
            UpdateMapPins();
        }
    }

    public bool HasValidCoordinates
    {
        get => _hasValidCoordinates;
        set
        {
            System.Diagnostics.Debug.WriteLine($"Setting HasValidCoordinates to: {value}");
            _hasValidCoordinates = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Pin> MapPins
    {
        get => _mapPins;
        set
        {
            _mapPins = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string IncidentId
    {
        get => _incidentId;
        set
        {
            _incidentId = value;
            LoadIncidentAsync().ConfigureAwait(false);
        }
    }

    public ObservableCollection<Polyline> RouteLines
    {
        get => _routeLines;
        set
        {
            _routeLines = value;
            OnPropertyChanged();
        }
    }

    public MapSpan MapSpan
    {
        get => _mapSpan;
        set
        {
            _mapSpan = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Location> Geopath
    {
        get => _geopath;
        set
        {
            _geopath = value;
            OnPropertyChanged();
        }
    }

    public Polyline RouteLine
    {
        get => _routeLine;
        set
        {
            _routeLine = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshCommand { get; }

    public ICommand ShowRouteCommand { get; }

    public event EventHandler<MapSpan> RegionChanged;

    public IncidentDetailsViewModel(IIncidentApi incidentApi, ITokenService tokenService, IDirectionsService directionsService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;
        _directionsService = directionsService;

        // Initialize the route line
        _routeLine = new Polyline
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 12
        };

        RefreshCommand = new Command(
            execute: async () => await LoadIncidentAsync(),
            canExecute: () => !IsLoading
        );

        ShowRouteCommand = new Command(
            execute: async () => await ShowRouteAsync(),
            canExecute: () => HasValidCoordinates
        );
    }

    private void UpdateMapPins()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Starting UpdateMapPins");
            MapPins.Clear();
            HasValidCoordinates = false;
            CurrentLocation = new Location(0, 0);

            if (Incident == null)
            {
                System.Diagnostics.Debug.WriteLine("Incident is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Raw coordinates - Latitude: {Incident.Latitude}, Longitude: {Incident.Longitude}");

            if (Incident.Latitude == 0 || Incident.Longitude == 0)
            {
                System.Diagnostics.Debug.WriteLine("Coordinates are 0,0 - invalid");
                return;
            }

            // Parse coordinates using invariant culture
            var latStr = Incident.Latitude.ToString("F6").Replace(",", ".");
            var lonStr = Incident.Longitude.ToString("F6").Replace(",", ".");

            System.Diagnostics.Debug.WriteLine($"Formatted coordinate strings - Lat: {latStr}, Lon: {lonStr}");

            var lat = Convert.ToDouble(latStr, CultureInfo.InvariantCulture);
            var lon = Convert.ToDouble(lonStr, CultureInfo.InvariantCulture);

            System.Diagnostics.Debug.WriteLine($"Parsed coordinates - Lat: {lat}, Lon: {lon}");

            var location = new Location(lat, lon);
            CurrentLocation = location;  // Set the current location for the static pin
            System.Diagnostics.Debug.WriteLine($"Created Location object - Lat: {location.Latitude}, Lon: {location.Longitude}");

            // Create a pin for the ItemsSource collection
            var pin = new Pin
            {
                Location = location,
                Label = $"Incident Location ({lat:F6}, {lon:F6})",
                Address = Incident.Address,
                Type = PinType.Place
            };
            MapPins.Add(pin);
            System.Diagnostics.Debug.WriteLine($"Added pin at exact location - Lat: {pin.Location.Latitude:F6}, Lon: {pin.Location.Longitude:F6}");

            HasValidCoordinates = true;
            System.Diagnostics.Debug.WriteLine("Completed UpdateMapPins successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in UpdateMapPins: {ex}");
            HasValidCoordinates = false;
            CurrentLocation = new Location(0, 0);
            ErrorMessage = $"Error updating map: {ex.Message}";
        }
    }

    private async Task LoadIncidentAsync()
    {
        if (string.IsNullOrEmpty(IncidentId))
        {
            ErrorMessage = "Invalid incident ID";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Please log in to view incident details.";
                return;
            }

            var response = await _incidentApi.GetIncidentByIdAsync(Guid.Parse(IncidentId));

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Incident = response.Content;
                ErrorMessage = string.Empty;
            }
            else if (response.Error != null)
            {
                ErrorMessage = $"Failed to load incident: {response.Error.Content}";
                System.Diagnostics.Debug.WriteLine($"Error loading incident: {response.Error.Content}");
            }
            else
            {
                ErrorMessage = "Failed to load incident. Please try again later.";
                System.Diagnostics.Debug.WriteLine("Error loading incident: Unknown error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception in LoadIncidentAsync: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateMapRegion(MapSpan span)
    {
        RegionChanged?.Invoke(this, span);
    }

    private async Task ShowRouteAsync()
    {
        try
        {
            var currentLocation = await _directionsService.GetCurrentLocationAsync();
            var incidentLocation = new Location(Incident.Latitude, Incident.Longitude);

            var routePoints = await _directionsService.GetRoutePointsAsync(currentLocation, incidentLocation);

            if (!routePoints.Any())
            {
                ErrorMessage = "Could not find a route to the incident location";
                return;
            }

            // Clear existing route points and add new ones
            Geopath.Clear();
            foreach (var point in routePoints)
            {
                Geopath.Add(point);
            }

            // Calculate the bounding box for the route
            var minLat = routePoints.Min(p => p.Latitude);
            var maxLat = routePoints.Max(p => p.Latitude);
            var minLon = routePoints.Min(p => p.Longitude);
            var maxLon = routePoints.Max(p => p.Longitude);

            // Create a MapSpan that includes all points with some padding
            var centerLat = (minLat + maxLat) / 2;
            var centerLon = (minLon + maxLon) / 2;
            var latitudeDelta = (maxLat - minLat) * 1.5; // 1.5 for some padding
            var longitudeDelta = (maxLon - minLon) * 1.5;

            UpdateMapRegion(new MapSpan(
                new Location(centerLat, centerLon),
                Math.Max(latitudeDelta, 0.01), // Minimum span of 0.01 degrees
                Math.Max(longitudeDelta, 0.01)
            ));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing route: {ex}");
            ErrorMessage = "Could not display the route";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}