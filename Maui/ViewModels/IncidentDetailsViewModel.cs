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
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Maui.ViewModels;

[QueryProperty(nameof(IncidentId), "id")]
public partial class IncidentDetailsViewModel : ObservableObject
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;
    private readonly IDirectionsService _directionsService;
    private readonly IApiConfigurationService _apiConfig;

    public event EventHandler<MapSpan>? RegionChanged;

    [ObservableProperty]
    private IncidentResponseDto? incident;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string incidentId = string.Empty;

    [ObservableProperty]
    private bool hasValidCoordinates;

    [ObservableProperty]
    private Location currentLocation = new Location(0, 0);

    [ObservableProperty]
    private MapSpan mapSpan;

    [ObservableProperty]
    private ObservableCollection<string> photos = new();

    [ObservableProperty]
    private bool hasPhotos;

    [ObservableProperty]
    private ObservableCollection<Location> geopath = new();

    [ObservableProperty]
    private Polyline routeLine = new()
    {
        StrokeColor = Colors.Blue,
        StrokeWidth = 12
    };

    public ICommand RefreshCommand { get; }
    public ICommand ShowRouteCommand { get; }

    public IncidentDetailsViewModel(
        IIncidentApi incidentApi,
        ITokenService tokenService,
        IDirectionsService directionsService,
        IApiConfigurationService apiConfig)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;
        _directionsService = directionsService;
        _apiConfig = apiConfig;

        Photos = new ObservableCollection<string>();

        RefreshCommand = new Command(
            execute: async () => await LoadIncidentAsync(Guid.Parse(IncidentId)),
            canExecute: () => !IsLoading
        );

        ShowRouteCommand = new Command(
            execute: async () => await ShowRouteAsync(),
            canExecute: () => HasValidCoordinates
        );

    }

    partial void OnIncidentChanged(IncidentResponseDto? value)
    {
        if (value != null)
        {

            HasValidCoordinates = value.Latitude != 0
                                && value.Longitude != 0
                                && Math.Abs(value.Latitude) <= 90
                                && Math.Abs(value.Longitude) <= 180;


            if (HasValidCoordinates)
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CurrentLocation = new Location(value.Latitude, value.Longitude);

                        // Create initial map span
                        MapSpan = MapSpan.FromCenterAndRadius(
                            CurrentLocation,
                            Distance.FromKilometers(0.5)
                        );
                        RegionChanged?.Invoke(this, MapSpan);
                    });
                }
                catch (Exception ex)
                {
                    HasValidCoordinates = false;
                }
            }
        }
    }

    partial void OnIncidentIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadIncidentAsync(Guid.Parse(value));
            });
        }
    }

    public async Task LoadIncidentAsync(Guid id)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var response = await _incidentApi.GetIncidentByIdAsync(id);

            if (response.IsSuccessStatusCode && response.Content != null)
            {

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Incident = response.Content;

                    // Load photos
                    if (response.Content.Photos != null && response.Content.Photos.Any())
                    {
                        Photos = new ObservableCollection<string>(
                            response.Content.Photos.Select(p => $"{_apiConfig.BaseUrl}/{p.FilePath.Replace("\\", "/")}")
                        );
                        HasPhotos = true;
                    }
                    else
                    {
                        Photos = new ObservableCollection<string>();
                        HasPhotos = false;
                    }
                });
            }
            else
            {
                ErrorMessage = "Failed to load incident details";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load incident details. Please try again.";
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
            if (Incident == null) return;

            var incidentLocation = new Location(Incident.Latitude, Incident.Longitude);
            var routePoints = await _directionsService.GetRoutePointsAsync(currentLocation, incidentLocation);

            if (!routePoints.Any())
            {
                ErrorMessage = "Could not find a route to the incident location";
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
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

                MapSpan = new MapSpan(
                    new Location(centerLat, centerLon),
                    Math.Max(latitudeDelta, 0.01), // Minimum span of 0.01 degrees
                    Math.Max(longitudeDelta, 0.01)
                );
                RegionChanged?.Invoke(this, MapSpan);
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = "Could not display the route";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}