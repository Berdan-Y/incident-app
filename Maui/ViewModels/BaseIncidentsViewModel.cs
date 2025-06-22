using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Maui.Pages;
using Maui.Services;
using Shared.Models.Dtos;

namespace Maui.ViewModels;

public abstract partial class BaseIncidentsViewModel : ObservableObject
{
    protected readonly IIncidentService _incidentService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage;

    [ObservableProperty]
    private string emptyMessage;

    [ObservableProperty]
    private ObservableCollection<IncidentResponseDto> incidents;

    public ICommand ViewIncidentDetailsCommand { get; protected set; }
    public ICommand EditIncidentCommand { get; protected set; }
    public ICommand DeleteIncidentCommand { get; protected set; }
    public ICommand FieldEmployeeEditCommand { get; protected set; }

    protected BaseIncidentsViewModel(IIncidentService incidentService)
    {
        _incidentService = incidentService;
        Incidents = new ObservableCollection<IncidentResponseDto>();
        EditIncidentCommand = new Command<IncidentResponseDto>(async (incident) => await OnEditIncident(incident));
        DeleteIncidentCommand = new Command<IncidentResponseDto>(async (incident) => await OnDeleteIncident(incident));
        FieldEmployeeEditCommand = new Command<IncidentResponseDto>(async (incident) => await OnFieldEmployeeEditIncident(incident));
    }

    protected virtual async Task OnEditIncident(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"/EditIncidentPage?id={incident.Id}");
    }

    protected virtual async Task OnDeleteIncident(IncidentResponseDto incident)
    {
        if (incident == null) return;

        bool answer = await Shell.Current.DisplayAlert(
            "Delete Incident",
            "Are you sure you want to delete this incident?",
            "Yes",
            "No");

        if (answer)
        {
            try
            {
                IsLoading = true;
                await _incidentService.DeleteIncidentAsync(incident);
                Incidents.Remove(incident);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete incident: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    protected virtual async Task OnFieldEmployeeEditIncident(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"/FieldEmployeeEditPage?id={incident.Id}");
    }
}