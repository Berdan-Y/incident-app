using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Shared.Api;
using Shared.Models.Dtos;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Shared.Services;

namespace Maui.Core.ViewModels;

public class PhotoItemBase : ObservableObject
{
    private byte[] _imageData = Array.Empty<byte>();
    public byte[] ImageData
    {
        get => _imageData;
        set => SetProperty(ref _imageData, value);
    }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public PhotoItemBase(byte[] imageData, string fileName, string contentType)
    {
        ImageData = imageData;
        FileName = fileName;
        ContentType = contentType;
    }
}

public abstract partial class ReportIncidentViewModelBase : ObservableObject
{
    protected readonly IIncidentApi _incidentApi;
    protected readonly ITokenService _tokenService;
    protected readonly IGeocodingService _geocodingService;

    [ObservableProperty]
    private ObservableCollection<PhotoItemBase> photos;

    [ObservableProperty]
    private bool isAnonymous;

    public bool IsLoggedIn => _tokenService.IsLoggedIn;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string zipcode = string.Empty;

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
    private string locationStatus = string.Empty;

    public bool IsNotLoading => !IsLoading;

    protected ReportIncidentViewModelBase(
        IIncidentApi incidentApi,
        ITokenService tokenService,
        IGeocodingService geocodingService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;
        _geocodingService = geocodingService;
        Photos = new ObservableCollection<PhotoItemBase>();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotLoading));
    }

    protected virtual async Task<(bool success, string message)> ValidateReport()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Title is required.");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required.");

        if (UseManualLocation)
        {
            if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Zipcode))
                return (false, "Address and Zipcode are required when setting location manually.");

            var geocodeResult = await _geocodingService.GeocodeAddressAsync(Address, Zipcode);
            if (!geocodeResult.success)
                return (false, geocodeResult.errorMessage ?? "Invalid address");

            Latitude = geocodeResult.latitude ?? 0;
            Longitude = geocodeResult.longitude ?? 0;
        }
        else if (UseCurrentLocation && Latitude == 0.0 && Longitude == 0.0)
        {
            return (false, "Location could not be determined. Please try again or use manual entry.");
        }

        return (true, string.Empty);
    }

    public async Task<(bool success, string message)> SubmitReportAsync()
    {
        try
        {
            IsLoading = true;

            var validation = await ValidateReport();
            if (!validation.success)
                return validation;

            Guid? reportedById = null;
            if (!IsAnonymous && IsLoggedIn)
            {
                var userId = _tokenService.GetUserId();
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid parsedId))
                {
                    reportedById = parsedId;
                }
            }

            var content = new MultipartFormDataContent();

            var incident = new IncidentCreateDto
            {
                Title = Title,
                Description = Description,
                Latitude = Latitude,
                Longitude = Longitude,
                Address = Address,
                ZipCode = Zipcode,
                ReportedById = reportedById
            };

            var incidentJson = JsonSerializer.Serialize(incident);
            content.Add(new StringContent(incidentJson, Encoding.UTF8, "application/json"), "incident");

            foreach (var photo in Photos)
            {
                var streamContent = new StreamContent(new MemoryStream(photo.ImageData));
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
                content.Add(streamContent, "photos", photo.FileName);
            }

            var response = await _incidentApi.CreateIncidentAsync(content);

            if (response.IsSuccessStatusCode)
            {
                ResetForm();
                return (true, "Incident report submitted successfully!");
            }
            else if (response.Error != null)
            {
                return (false, $"Something went wrong - {response.Error.Content} - {response.Error.StatusCode}");
            }

            return (false, "An unexpected error occurred. Please try again later.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to submit report: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual void ResetForm()
    {
        Title = string.Empty;
        Description = string.Empty;
        if (UseManualLocation)
        {
            Address = string.Empty;
            Zipcode = string.Empty;
        }
        IsAnonymous = false;
        Photos.Clear();
    }

    public async Task<(bool success, string message)> ValidateAndGeocodeAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Zipcode))
            return (false, "Please enter both address and zipcode.");

        try
        {
            IsLoading = true;
            LocationStatus = "Validating address...";

            var result = await _geocodingService.GeocodeAddressAsync(Address, Zipcode);
            if (!result.success)
                return (false, result.errorMessage ?? "Failed to validate address.");

            Latitude = result.latitude ?? 0;
            Longitude = result.longitude ?? 0;
            LocationStatus = "Address validated successfully!";
            return (true, "Address validated successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Failed to validate address. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public interface IGeocodingService
{
    Task<(bool success, double? latitude, double? longitude, string? errorMessage)> GeocodeAddressAsync(string address, string zipCode);
    Task<(bool success, string? address, string? zipCode, string? errorMessage)> ReverseGeocodeAsync(double latitude, double longitude);
}