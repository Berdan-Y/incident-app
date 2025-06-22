using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Pages;
using Maui.Services;
using Shared.Models.Dtos;
using Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace Maui.ViewModels;

public class FilterOption<T> where T : struct
{
    public T? Value { get; set; }
    public string DisplayName { get; set; }

    public FilterOption(T? value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}

public partial class AssignedIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly IConnectivity _connectivity;
    private ObservableCollection<IncidentResponseDto> _unfilteredIncidents;

    [ObservableProperty]
    private FilterOption<Status> selectedStatusFilter;

    [ObservableProperty]
    private FilterOption<Priority> selectedPriorityFilter;

    public ObservableCollection<FilterOption<Status>> StatusOptions { get; }
    public ObservableCollection<FilterOption<Priority>> PriorityOptions { get; }

    public AssignedIncidentsViewModel(IIncidentService incidentService, IConnectivity connectivity)
        : base(incidentService)
    {
        _connectivity = connectivity;
        _unfilteredIncidents = new ObservableCollection<IncidentResponseDto>();
        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));
        EmptyMessage = "No incidents have been assigned to you yet.";

        // Initialize Status filter options
        StatusOptions = new ObservableCollection<FilterOption<Status>>();
        StatusOptions.Add(new FilterOption<Status>(null, "All"));
        foreach (Status status in Enum.GetValues<Status>())
        {
            StatusOptions.Add(new FilterOption<Status>(status, status.ToString()));
        }

        // Initialize Priority filter options
        PriorityOptions = new ObservableCollection<FilterOption<Priority>>();
        PriorityOptions.Add(new FilterOption<Priority>(null, "All"));
        foreach (Priority priority in Enum.GetValues<Priority>())
        {
            PriorityOptions.Add(new FilterOption<Priority>(priority, priority.ToString()));
        }

        // Set default selections to "All"
        SelectedStatusFilter = StatusOptions[0];
        SelectedPriorityFilter = PriorityOptions[0];
    }

    private async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    partial void OnSelectedStatusFilterChanged(FilterOption<Status> value)
    {
        ApplyFilters();
    }

    partial void OnSelectedPriorityFilterChanged(FilterOption<Priority> value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filteredIncidents = _unfilteredIncidents.AsEnumerable();

        if (SelectedStatusFilter?.Value.HasValue == true)
        {
            filteredIncidents = filteredIncidents.Where(i => i.Status == SelectedStatusFilter.Value.Value);
        }

        if (SelectedPriorityFilter?.Value.HasValue == true)
        {
            filteredIncidents = filteredIncidents.Where(i => i.Priority == SelectedPriorityFilter.Value.Value);
        }

        Incidents.Clear();
        foreach (var incident in filteredIncidents)
        {
            Incidents.Add(incident);
        }
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

            _unfilteredIncidents.Clear();
            foreach (var incident in incidents)
            {
                _unfilteredIncidents.Add(incident);
            }

            ApplyFilters();
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

    protected override async Task OnEditIncident(IncidentResponseDto incident)
    {
        if (incident == null) return;

        // Since this is the AssignedIncidentsPage, we know the user is a Field Employee
        // and these are their assigned incidents, so we can directly navigate to edit
        await Shell.Current.GoToAsync($"/EditIncidentPage?id={incident.Id}");
    }
}