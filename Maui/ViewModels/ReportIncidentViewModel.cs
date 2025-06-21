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
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Media;

namespace Maui.ViewModels;

public partial class ReportIncidentViewModel : ObservableObject, IDisposable
{
    private readonly IGeolocation _geolocation;
    private readonly IMap _map;
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;
    private readonly IGeocodingService _geocodingService;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<FileResult> selectedPhotos;

    [ObservableProperty]
    private bool isAnonymous;

    partial void OnIsAnonymousChanged(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"IsAnonymous changed to: {value}");
    }

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

    public ReportIncidentViewModel(
        IGeolocation geolocation,
        IMap map,
        IIncidentApi incidentApi,
        ITokenService tokenService,
        IGeocodingService geocodingService)
    {
        _geolocation = geolocation;
        _map = map;
        _incidentApi = incidentApi;
        _tokenService = tokenService;
        _geocodingService = geocodingService;

        // Initialize photos collection
        SelectedPhotos = new ObservableCollection<FileResult>();

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

                // Get address from coordinates
                var reverseGeocodeResult = await _geocodingService.ReverseGeocodeAsync(Latitude, Longitude);
                if (reverseGeocodeResult.success)
                {
                    Address = reverseGeocodeResult.address;
                    Zipcode = reverseGeocodeResult.zipCode;
                    LocationStatus = "Location and address details retrieved successfully!";
                }
                else
                {
                    Address = null;
                    Zipcode = null;
                    LocationStatus = "Got location coordinates, but couldn't retrieve address details.";
                }

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

    private async Task ValidateAndGeocodeAddress()
    {
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Zipcode))
        {
            LocationStatus = "Please enter both address and zipcode.";
            return;
        }

        try
        {
            IsLoading = true;
            LocationStatus = "Validating address...";
            ShowMap = false;

            var result = await _geocodingService.GeocodeAddressAsync(Address, Zipcode);

            if (!result.success)
            {
                LocationStatus = result.errorMessage ?? "Failed to validate address.";
                await Application.Current.MainPage.DisplayAlert("Invalid Address",
                    "The provided address could not be found. Please check and try again.", "OK");
                return;
            }

            Latitude = result.latitude ?? 0;
            Longitude = result.longitude ?? 0;

            MapPins.Clear();
            var pin = new Pin
            {
                Location = new Location(Latitude, Longitude),
                Label = "Incident Location",
                Type = PinType.Generic
            };
            MapPins.Add(pin);

            InitialMapPosition = MapSpan.FromCenterAndRadius(
                new Location(Latitude, Longitude),
                Distance.FromKilometers(0.5)
            );

            await Task.Delay(100); // Small delay to ensure binding updates
            ShowMap = true;
            LocationStatus = "Address validated successfully!";
        }
        catch (Exception ex)
        {
            LocationStatus = "Failed to validate address. Please try again.";
            await Application.Current.MainPage.DisplayAlert("Error",
                "Failed to validate address. Please try again.", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnAddressChanged(string value)
    {
        if (UseManualLocation && !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(Zipcode))
        {
            MainThread.BeginInvokeOnMainThread(async () => await ValidateAndGeocodeAddress());
        }
    }

    partial void OnZipcodeChanged(string value)
    {
        if (UseManualLocation && !string.IsNullOrWhiteSpace(Address) && !string.IsNullOrWhiteSpace(value))
        {
            MainThread.BeginInvokeOnMainThread(async () => await ValidateAndGeocodeAddress());
        }
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Application.Current.MainPage.DisplayAlert("Error", 
                    "Camera capture is not supported on this device.", "OK");
                return;
            }

            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Permission Required", 
                    "Camera permission is required to take photos.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                SelectedPhotos.Add(photo);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", 
                $"Failed to take photo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task PickPhotos()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Photos>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Permission Required", 
                    "Photo library access is required to select photos.", "OK");
                return;
            }

            var photos = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images
            });

            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    SelectedPhotos.Add(photo);
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", 
                $"Failed to pick photos: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void RemovePhoto(FileResult photo)
    {
        if (photo != null && SelectedPhotos.Contains(photo))
        {
            SelectedPhotos.Remove(photo);
        }
    }

    [RelayCommand]
    private async Task SubmitReport()
    {
        System.Diagnostics.Debug.WriteLine($"SubmitReport called - IsAnonymous: {IsAnonymous}");

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

        if (UseManualLocation)
        {
            if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Zipcode))
            {
                await Application.Current.MainPage.DisplayAlert("Validation Error",
                    "Address and Zipcode are required when setting location manually.", "OK");
                return;
            }

            // Validate address before submission
            var geocodeResult = await _geocodingService.GeocodeAddressAsync(Address, Zipcode);
            if (!geocodeResult.success)
            {
                await Application.Current.MainPage.DisplayAlert("Invalid Address",
                    geocodeResult.errorMessage ?? "The provided address is invalid. Please check and try again.", "OK");
                return;
            }

            Latitude = geocodeResult.latitude ?? 0;
            Longitude = geocodeResult.longitude ?? 0;
        }

        try
        {
            IsLoading = true;
            Guid? reportedById = null;

            System.Diagnostics.Debug.WriteLine($"SubmitReport - IsAnonymous: {IsAnonymous}, IsLoggedIn: {IsLoggedIn}");

            // Only set reportedById if not anonymous and logged in
            if (!IsAnonymous && IsLoggedIn)
            {
                var userId = _tokenService.GetUserId();
                System.Diagnostics.Debug.WriteLine($"Non-anonymous report - userId from token: {userId}");
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid parsedId))
                {
                    reportedById = parsedId;
                    System.Diagnostics.Debug.WriteLine($"Setting reportedById to: {parsedId}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Final reportedById value: {reportedById}");

            // Create multipart form content
            var content = new MultipartFormDataContent();

            // Add incident data as JSON
            var incident = new IncidentCreateDto()
            {
                Title = Title,
                Description = Description,
                Latitude = Latitude,
                Longitude = Longitude,
                Address = Address,
                ZipCode = Zipcode,
                ReportedById = reportedById  // Will be null for anonymous reports
            };

            var incidentJson = JsonSerializer.Serialize(incident);
            content.Add(new StringContent(incidentJson, Encoding.UTF8, "application/json"), "incident");

            // Add photos
            foreach (var photo in SelectedPhotos)
            {
                var stream = await photo.OpenReadAsync();
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
                content.Add(streamContent, "photos", photo.FileName);
            }

            var response = await _incidentApi.CreateIncidentAsync(content);

            if (response.IsSuccessStatusCode)
            {
                // Reset only title and description
                Title = string.Empty;
                Description = string.Empty;
                
                // Only reset address and zipcode if using manual location
                if (UseManualLocation)
                {
                    Address = string.Empty;
                    Zipcode = string.Empty;
                }
                
                IsAnonymous = false;  // Reset anonymous flag
                SelectedPhotos.Clear();

                await Application.Current.MainPage.DisplayAlert("Success",
                    "Incident report submitted successfully!", "OK");
            }
            else if (response.Error != null)
            {
                await Application.Current.MainPage.DisplayAlert("Submission Failed",
                    $"Something went wrong - {response.Error.Content} - {response.Error.StatusCode}", "OK");
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