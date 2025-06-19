using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Maui.Pages;

namespace Maui.ViewModels;

public partial class AllIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;

    public AllIncidentsViewModel(IIncidentApi incidentApi, ITokenService tokenService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;

        EmptyMessage = "No incidents have been reported yet.";
        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));

        // Load incidents when the ViewModel is created
        MainThread.BeginInvokeOnMainThread(async () => await LoadIncidents());
    }

    private async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    [RelayCommand]
    private async Task LoadIncidents()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Ensure we have a valid token
            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Please log in to view incidents.";
                return;
            }

            var response = await _incidentApi.GetIncidentsAsync();
            Debug.WriteLine($"Response received - IsSuccessStatusCode: {response.IsSuccessStatusCode}, StatusCode: {response.StatusCode}");

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Incidents.Clear();
                foreach (var incident in response.Content)
                {
                    Debug.WriteLine($"Adding incident: ID={incident.Id}, Title={incident.Title}, Status={incident.Status}");
                    Incidents.Add(incident);
                }
                ErrorMessage = string.Empty;
            }
            else if (response.Error != null)
            {
                ErrorMessage = $"Failed to load incidents: {response.Error.Content}";
                Debug.WriteLine($"Error loading incidents: {response.Error.Content}");
            }
            else
            {
                ErrorMessage = "Failed to load incidents. Please try again later.";
                Debug.WriteLine("Error loading incidents: Unknown error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load incidents: {ex.Message}";
            Debug.WriteLine($"Exception in LoadIncidents: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OnAppearing()
    {
        await LoadIncidents();
    }
} 