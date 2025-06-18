using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Api;
using Shared.Models.Dtos;
using System.Collections.ObjectModel;

namespace Maui.ViewModels;

public partial class AllIncidentsViewModel : ObservableObject
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<IncidentResponseDto> _incidents;

    public AllIncidentsViewModel(IIncidentApi incidentApi, ITokenService tokenService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;
        _incidents = new ObservableCollection<IncidentResponseDto>();
    }

    [RelayCommand]
    private async Task LoadIncidents()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var response = await _incidentApi.GetIncidentsAsync();
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Incidents.Clear();
                foreach (var incident in response.Content)
                {
                    Incidents.Add(incident);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to load incidents: " + ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewIncident(Guid id)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Id", id }
        };
        await Shell.Current.GoToAsync("IncidentDetailsPage", parameters);
    }

    public async Task OnAppearing()
    {
        await LoadIncidents();
    }
} 