using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Maui.Services;
using Microsoft.Maui.Controls.Maps;
using IMap = Microsoft.Maui.Maps.IMap;
using Microsoft.Maui.Controls;
using Shared.Api;
using Shared.Models.Dtos;
using Shared.Models.Enums;

namespace Maui.ViewModels;

public partial class ReportIncidentViewModel : ObservableObject, IDisposable
{
    private readonly IGeolocation _geolocation;
    private readonly IMap _map;
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;
    private bool _disposed;

    [ObservableProperty]
    private bool isAnonymous;

    public bool IsLoggedIn => _tokenService.IsLoggedIn;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string description;

    [ObservableProperty]
    private string address;

    [ObservableProperty]
    private string zipcode;

    [ObservableProperty]
    private bool useCurrentLocation = true;

    [ObservableProperty]
    private bool useManualLocation;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private double latitude;

    [ObservableProperty]
    private double longitude;

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins;

    [ObservableProperty]
    private string locationStatus;

    [ObservableProperty]
    private bool showMap;

    [ObservableProperty]
    private MapSpan initialMapPosition;

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotLoading));
    }

    public bool IsNotLoading => !IsLoading;

    public ReportIncidentViewModel(IGeolocation geolocation, IMap map, IIncidentApi incidentApi, ITokenService tokenService)
    {
        _geolocation = geolocation;
        _map = map;
        _incidentApi = incidentApi;
        _tokenService = tokenService;

        // Subscribe to token service property changes
        _tokenService.PropertyChanged += TokenService_PropertyChanged;

        MapPins = new ObservableCollection<Pin>();
        ShowMap = false;

        if (useCurrentLocation)
        {
            MainThread.BeginInvokeOnMainThread(async () => await RequestLocationPermission());
        }
    }

    private void TokenService_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITokenService.IsLoggedIn))
        {
            OnPropertyChanged(nameof(IsLoggedIn));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _tokenService.PropertyChanged -= TokenService_PropertyChanged;
            }
            _disposed = true;
        }
    }

    private async Task RequestLocationPermission()
    {
        try
        {
            LocationStatus = "Requesting location permission...";
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    LocationStatus = "Location permission denied. Please enter location manually.";
                    await Application.Current.MainPage.DisplayAlert("Permission Required",
                        "Location permission is required to use automatic location. Please enable it in settings.", "OK");
                    UseManualLocation = true;
                    return;
                }
            }

            await GetCurrentLocation();
        }
        catch (Exception ex)
        {
            LocationStatus = "Failed to get location permission. Please try manual entry.";
            await Application.Current.MainPage.DisplayAlert("Error",
                "Failed to request location permission. Please try again or use manual entry.", "OK");
            UseManualLocation = true;
        }
    }

    partial void OnUseCurrentLocationChanged(bool value)
    {
        if (value)
        {
            UseManualLocation = false;
            RequestLocationPermission();
        }
        else
        {
            Latitude = 0.0;
            Longitude = 0.0;
            MapPins.Clear();
            LocationStatus = string.Empty;
        }
    }

    partial void OnUseManualLocationChanged(bool value)
    {
        if (value)
        {
            UseCurrentLocation = false;
            Latitude = 0.0;
            Longitude = 0.0;
            MapPins.Clear();
            LocationStatus = string.Empty;
        }
    }

    private async Task GetCurrentLocation()
    {
        try
        {
            IsLoading = true;
            LocationStatus = "Fetching your location...";
            ShowMap = false;
            InitialMapPosition = null;

            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(5)
            };

            var location = await _geolocation.GetLocationAsync(request);

            if (location != null)
            {
                Latitude = location.Latitude;
                Longitude = location.Longitude;
                Address = null;
                Zipcode = null;

                MapPins.Clear();
                var pin = new Pin
                {
                    Location = new Location(Latitude, Longitude),
                    Label = "Incident Location",
                    Type = PinType.Generic
                };
                MapPins.Add(pin);

                // Set the initial map position before showing the map
                InitialMapPosition = MapSpan.FromCenterAndRadius(
                    new Location(Latitude, Longitude),
                    Distance.FromKilometers(0.5)
                );

                await Task.Delay(100); // Small delay to ensure binding updates
                ShowMap = true;
                LocationStatus = "We got your location!";
            }
            else
            {
                LocationStatus = "Failed to get your location. Please try to manually add the location.";
                UseManualLocation = true;
            }
        }
        catch (Exception ex)
        {
            LocationStatus = "Failed to get your location. Please try to manually add the location.";
            await Application.Current.MainPage.DisplayAlert("Error",
                "Failed to get location. Please try again or use manual entry.", "OK");
            UseManualLocation = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SubmitReport()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error", "Title is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error", "Description is required.", "OK");
            return;
        }

        if (UseCurrentLocation && Latitude == 0.0 && Longitude == 0.0)
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error",
                "Location could not be determined. Please try again or use manual entry.", "OK");
            return;
        }

        if (UseManualLocation && (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Zipcode)))
        {
            await Application.Current.MainPage.DisplayAlert("Validation Error",
                "Address and Zipcode are required when setting location manually.", "OK");
            return;
        }

        try
        {
            IsLoading = true;
            Guid? reportedById = null;

            if (IsLoggedIn && !IsAnonymous)
            {
                var userId = _tokenService.GetUserId();
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid parsedId))
                {
                    reportedById = parsedId;
                }
            }

            var incident = new IncidentCreateDto()
            {
                Title = Title,
                Description = Description,
                Latitude = UseCurrentLocation ? Latitude : null,
                Longitude = UseCurrentLocation ? Longitude : 0.0,
                Address = UseManualLocation ? Address : null,
                ZipCode = UseManualLocation ? Zipcode : null,
                ReportedById = reportedById
            };

            var response = await _incidentApi.CreateIncidentAsync(incident);

            if (response.IsSuccessStatusCode)
            {
                // Reset fields after successful submission
                Title = string.Empty;
                Description = string.Empty;
                Address = string.Empty;
                Zipcode = string.Empty;

                await Application.Current.MainPage.DisplayAlert("Success",
                    "Incident report submitted successfully!", "OK");
            }
            else if (response.Error != null)
            {
                await Application.Current.MainPage.DisplayAlert("Submission Failed", $"Something went wrong - {response.Error.Content} - {response.Error.StatusCode}", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Submission Failed",
                    "An unexpected error occurred. Please try again later.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error",
                $"Failed to submit report. Please try again. - {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}