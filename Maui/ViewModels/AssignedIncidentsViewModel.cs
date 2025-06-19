using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Pages;
using Maui.Services;
using Shared.Models.Dtos;

namespace Maui.ViewModels;

public partial class AssignedIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly IConnectivity _connectivity;

    public AssignedIncidentsViewModel(IIncidentService incidentService, IConnectivity connectivity)
        : base(incidentService)
    {
        _connectivity = connectivity;
        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));
        EmptyMessage = "No incidents have been assigned to you yet.";
    }

    private async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    [RelayCommand]
    private async Task LoadIncidents()
    {
        if (IsLoading)
            return;

        try
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("No Internet",
                    "Please check your internet connection and try again.", "OK");
                return;
            }

            IsLoading = true;
            var incidents = await _incidentService.GetAssignedIncidentsAsync();
            Incidents.Clear();
            foreach (var incident in incidents)
            {
                Incidents.Add(incident);
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