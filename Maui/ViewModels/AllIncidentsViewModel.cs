using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Models.Dtos;
using Maui.Pages;

namespace Maui.ViewModels;

public partial class AllIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly ITokenService _tokenService;

    public AllIncidentsViewModel(IIncidentService incidentService, ITokenService tokenService)
        : base(incidentService)
    {
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

            var incidents = await _incidentService.GetAllIncidentsAsync();
            Incidents.Clear();
            foreach (var incident in incidents)
            {
                Incidents.Add(incident);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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