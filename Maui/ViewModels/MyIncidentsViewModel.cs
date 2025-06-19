using Maui.Services;
using Shared.Api;
using System.Collections.ObjectModel;
using Shared.Models.Dtos;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Input;
using Maui.Pages;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Maui.ViewModels;

public partial class MyIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly IIncidentApi _incidentApi;
    private readonly ITokenService _tokenService;

    public ICommand LoadMyReportsCommand { get; }

    public MyIncidentsViewModel(IIncidentApi incidentApi, ITokenService tokenService)
    {
        _incidentApi = incidentApi;
        _tokenService = tokenService;

        LoadMyReportsCommand = new Command(
            execute: async () => await LoadMyReportsAsync(),
            canExecute: () => !IsLoading
        );

        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));

        EmptyMessage = "You haven't reported any incidents yet. Tap the 'Report Incident' tab to create one.";

        // Load incidents when the ViewModel is created
        MainThread.BeginInvokeOnMainThread(async () => await LoadMyReportsAsync());
    }

    private async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    public async Task LoadMyReportsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Ensure we have a valid token
            var token = _tokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Please log in to view your incidents.";
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Loading incidents with token: {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");
            var response = await _incidentApi.GetMyIncidentsAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Response received - IsSuccessStatusCode: {response.IsSuccessStatusCode}, StatusCode: {response.StatusCode}");

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Incidents.Clear();
                foreach (var incident in response.Content)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding incident: ID={incident.Id}, Title={incident.Title}, Status={incident.Status}");
                    Incidents.Add(incident);
                }
                ErrorMessage = string.Empty;
            }
            else if (response.Error != null)
            {
                ErrorMessage = $"Failed to load incidents: {response.Error.Content}";
                System.Diagnostics.Debug.WriteLine($"Error loading incidents: {response.Error.Content}");
            }
            else
            {
                ErrorMessage = "Failed to load incidents. Please try again later.";
                System.Diagnostics.Debug.WriteLine("Error loading incidents: Unknown error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception in LoadMyReportsAsync: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}