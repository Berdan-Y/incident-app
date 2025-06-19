using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;

namespace Maui.ViewModels;

public partial class EditIncidentViewModel : ObservableObject
{
    private readonly IIncidentApi _incidentApi;
    private readonly IGeocodingService _geocodingService;
    private Guid _incidentId;

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

    public EditIncidentViewModel(IIncidentApi incidentApi, IGeocodingService geocodingService)
    {
        _incidentApi = incidentApi;
        _geocodingService = geocodingService;
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
                IsAddressValid = true;
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

    [RelayCommand]
    private async Task ValidateAddress()
    {
        if (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(ZipCode))
        {
            IsAddressValid = false;
            AddressValidationMessage = "Both address and zip code are required";
            return;
        }

        try
        {
            IsLoading = true;
            var result = await _geocodingService.GeocodeAddressAsync(Address, ZipCode);

            if (!result.success)
            {
                IsAddressValid = false;
                AddressValidationMessage = result.errorMessage ?? "Could not validate this address. Please check and try again.";
            }
            else
            {
                IsAddressValid = true;
                AddressValidationMessage = "Address validated successfully";
            }
        }
        catch (Exception ex)
        {
            IsAddressValid = false;
            AddressValidationMessage = $"Address validation failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description))
        {
            await Shell.Current.DisplayAlert("Validation Error", "Title and Description are required", "OK");
            return;
        }

        if (!IsAddressValid)
        {
            await Shell.Current.DisplayAlert("Validation Error", "Please validate the address first", "OK");
            return;
        }

        try
        {
            IsLoading = true;

            // Get coordinates for the address
            var geocodeResult = await _geocodingService.GeocodeAddressAsync(Address, ZipCode);
            if (!geocodeResult.success)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to geocode the address", "OK");
                return;
            }

            var updateDto = new UpdateIncidentDto
            {
                Title = Title,
                Description = Description,
                Address = Address,
                ZipCode = ZipCode,
                Latitude = geocodeResult.latitude ?? 0,
                Longitude = geocodeResult.longitude ?? 0
            };

            var response = await _incidentApi.UpdateIncidentAsync(_incidentId, updateDto);
            if (response.IsSuccessStatusCode)
            {
                await Shell.Current.DisplayAlert("Success", "Incident updated successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to update incident", "OK");
            }
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