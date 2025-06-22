using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Models.Dtos;
using Maui.Pages;
using System.Diagnostics;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Maui.ViewModels;

public partial class AllIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private bool isListView = true;

    [ObservableProperty]
    private bool isMapView;

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins = new();

    public AllIncidentsViewModel(IIncidentService incidentService, ITokenService tokenService)
        : base(incidentService)
    {
        _tokenService = tokenService;

        EmptyMessage = "No incidents have been reported yet.";
        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));

        // Load incidents when the ViewModel is created
        MainThread.BeginInvokeOnMainThread(async () => await LoadIncidents());
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsListView = !IsListView;
        IsMapView = !IsMapView;
        
        if (IsMapView)
        {
            UpdateMapPins();
        }
    }

    private void UpdateMapPins()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapPins.Clear();
            foreach (var incident in Incidents)
            {
                if (incident.Latitude != 0 && incident.Longitude != 0)
                {
                    var pin = new Pin
                    {
                        Location = new Location(incident.Latitude, incident.Longitude),
                        Label = incident.Title,
                        Address = incident.Address,
                        Type = PinType.Place
                    };
                    
                    // Add tap gesture
                    pin.MarkerClicked += async (s, e) =>
                    {
                        await OnViewIncidentDetails(incident);
                    };
                    
                    MapPins.Add(pin);
                }
            }
        });
    }

    public async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    private async Task HandleUnauthorizedAccess()
    {
        await _tokenService.LogoutAsync();
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.DisplayAlert("Session Expired", "Your session has expired. Please log in again.", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        });
    }

    [RelayCommand]
    private async Task LoadIncidents()
    {
        if (IsLoading) return;

        try
        {
            Debug.WriteLine("LoadIncidents started");
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Ensure we have a valid token
            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Please log in to view incidents.";
                Incidents.Clear(); // Clear the collection if not authenticated
                await HandleUnauthorizedAccess();
                return;
            }

            var incidents = await _incidentService.GetAllIncidentsAsync();
            
            // Clear and update the collection on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Incidents.Clear();
                if (incidents?.Any() == true)
                {
                    foreach (var incident in incidents)
                    {
                        Incidents.Add(incident);
                    }
                }
                
                // Update map pins if in map view
                if (IsMapView)
                {
                    UpdateMapPins();
                }
                
                Debug.WriteLine($"Loaded {incidents?.Count ?? 0} incidents");
            });
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("Unauthorized access - redirecting to login");
            await HandleUnauthorizedAccess();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in LoadIncidents: {ex}");
            ErrorMessage = ex.Message;
            MainThread.BeginInvokeOnMainThread(() => 
            {
                Incidents.Clear();
                MapPins.Clear();
            }); // Clear on error
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