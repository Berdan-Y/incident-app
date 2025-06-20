using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Microsoft.Maui.Controls.Maps;
using Shared.Api;
using Shared.Models.Dtos;
using System.Collections.ObjectModel;

namespace Maui.ViewModels;

public partial class EditIncidentViewModel : ObservableObject
{
    private readonly IIncidentApi _incidentApi;
    private readonly IGeocodingService _geocodingService;
    private Guid _incidentId;
    private string? _reportedByUserId;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string description;

    [ObservableProperty]
    private string address;

    [ObservableProperty]
    private string zipCode;

    [ObservableProperty]
    private bool isAddressValid;

    [ObservableProperty]
    private string addressValidationMessage;

    [ObservableProperty]
    private bool showMap;

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins;

    private double? currentLatitude;
    private double? currentLongitude;

    private double? _originalLatitude;
    private double? _originalLongitude;
    private string? _originalAddress;
    private string? _originalZipCode;

    public bool CanSave => !IsLoading && 
                          !string.IsNullOrWhiteSpace(Title) && 
                          !string.IsNullOrWhiteSpace(Description) && 
                          IsAddressValid;

    public EditIncidentViewModel(IIncidentApi incidentApi, IGeocodingService geocodingService)
    {
        _incidentApi = incidentApi;
        _geocodingService = geocodingService;
        MapPins = new ObservableCollection<Pin>();

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(IsLoading) or nameof(Title) or 
                nameof(Description) or nameof(IsAddressValid))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        };
    }

    public async Task LoadIncident(Guid id)
    {
        try
        {
            IsLoading = true;
            _incidentId = id;

            var response = await _incidentApi.GetIncidentByIdAsync(id);
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Title = response.Content.Title;
                Description = response.Content.Description;
                Address = response.Content.Address ?? string.Empty;
                ZipCode = response.Content.ZipCode ?? string.Empty;
                _reportedByUserId = response.Content.CreatedBy?.Id;
                
                // Store original values
                _originalAddress = Address;
                _originalZipCode = ZipCode;
                _originalLatitude = response.Content.Latitude;
                _originalLongitude = response.Content.Longitude;
                
                // If we have coordinates, show them on the map
                if (response.Content.Latitude != 0 && response.Content.Longitude != 0)
                {
                    currentLatitude = response.Content.Latitude;
                    currentLongitude = response.Content.Longitude;
                    await UpdateMapPins(response.Content.Latitude, response.Content.Longitude);
                    ShowMap = true;
                }

                IsAddressValid = !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(ZipCode);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load incident details", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateMapPins(double latitude, double longitude)
    {
        try
        {
            var pin = new Pin
            {
                Location = new Location(latitude, longitude),
                Label = "Incident Location",
                Type = PinType.Place
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapPins.Clear();
                MapPins.Add(pin);
            });
        }
        catch (Exception ex)
        {
            AddressValidationMessage = $"Error updating map: {ex.Message}";
        }
    }

    private string GetUserFriendlyErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "Could not validate this address. Please check and try again.";

        // Clean up the error message
        var cleanedMessage = errorMessage
            .Replace("Geocoding Failed:", "")
            .Replace("Geocoding failed:", "")
            .Trim();

        return cleanedMessage.ToUpperInvariant() switch
        {
            "ZERO_RESULTS" => "No matching address found. Please check the address and try again.",
            "INVALID_REQUEST" => "The address information is incomplete. Please provide both street address and zip code.",
            "REQUEST_DENIED" => "Unable to validate address at this time. Please try again later.",
            "OVER_QUERY_LIMIT" => "Address validation service is temporarily unavailable. Please try again later.",
            "UNKNOWN_ERROR" => "An unexpected error occurred. Please try again later.",
            _ => $"Address validation failed: {cleanedMessage}"
        };
    }

    [RelayCommand]
    private async Task ValidateAddress()
    {
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(ZipCode))
        {
            IsAddressValid = false;
            AddressValidationMessage = "Both address and zip code are required";
            ShowMap = false;
            return;
        }

        try
        {
            IsLoading = true;
            AddressValidationMessage = "Validating address...";
            var result = await _geocodingService.GeocodeAddressAsync(Address, ZipCode);

            if (!result.success)
            {
                IsAddressValid = false;
                AddressValidationMessage = GetUserFriendlyErrorMessage(result.errorMessage);
                ShowMap = false;
            }
            else if (result.latitude == null || result.longitude == null)
            {
                IsAddressValid = false;
                AddressValidationMessage = "Address found but coordinates are invalid. Please try a different address.";
                ShowMap = false;
            }
            else
            {
                IsAddressValid = true;
                AddressValidationMessage = $"Address validated: {Address}, {ZipCode}";
                currentLatitude = result.latitude.Value;
                currentLongitude = result.longitude.Value;
                await UpdateMapPins(result.latitude.Value, result.longitude.Value);
                ShowMap = true;
            }
        }
        catch (Exception ex)
        {
            IsAddressValid = false;
            AddressValidationMessage = $"Address validation failed: {ex.Message}";
            ShowMap = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsLoading || !CanSave)
            return;

        try
        {
            IsLoading = true;

            // Only geocode if address has changed
            if (Address != _originalAddress || ZipCode != _originalZipCode)
            {
                var geocodeResult = await _geocodingService.GeocodeAddressAsync(Address, ZipCode);
                if (!geocodeResult.success)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to geocode the address", "OK");
                    return;
                }

                var updateDto = new UpdateIncidentDetailsDto
                {
                    Title = Title,
                    Description = Description,
                    Address = Address,
                    ZipCode = ZipCode,
                    Latitude = geocodeResult.latitude ?? 0,
                    Longitude = geocodeResult.longitude ?? 0
                };

                var response = await _incidentApi.UpdateIncidentDetailsAsync(_incidentId, updateDto);
                if (!response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to update Incident", "OK");
                    return;
                }
            }
            else
            {
                // If address hasn't changed, just update title and description
                var updateDto = new UpdateIncidentDetailsDto
                {
                    Title = Title,
                    Description = Description,
                    Address = Address,
                    ZipCode = ZipCode,
                    Latitude = _originalLatitude ?? 0,
                    Longitude = _originalLongitude ?? 0
                };

                var response = await _incidentApi.UpdateIncidentDetailsAsync(_incidentId, updateDto);
                if (!response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to update Incident", "OK");
                    return;
                }
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}