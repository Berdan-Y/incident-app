using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Controls.Maps;
using IMap = Microsoft.Maui.Maps.IMap;
using Microsoft.Maui.Controls;

namespace Maui.ViewModels;

public partial class ReportIncidentViewModel : ObservableObject
{
    private readonly IGeolocation _geolocation;
    private readonly IMap _map;

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

    public bool IsNotLoading => !IsLoading;

    public ReportIncidentViewModel(IGeolocation geolocation, IMap map)
    {
        _geolocation = geolocation;
        _map = map;
        MapPins = new ObservableCollection<Pin>();
        ShowMap = false;

        if (useCurrentLocation)
        {
            MainThread.BeginInvokeOnMainThread(async () => await RequestLocationPermission());
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
            // TODO: Implement your API call here to submit the report
            // await _apiService.SubmitReport(new ReportModel { ... });
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error",
                "Failed to submit report. Please try again.", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}