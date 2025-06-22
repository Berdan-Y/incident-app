using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Shared.Models.Dtos;
using System.Windows.Input;
using Maui.Pages;
using System.Diagnostics;

namespace Maui.ViewModels;

public partial class MyIncidentsViewModel : BaseIncidentsViewModel
{
    private readonly ITokenService _tokenService;

    public MyIncidentsViewModel(IIncidentService incidentService, ITokenService tokenService)
        : base(incidentService)
    {
        _tokenService = tokenService;

        EmptyMessage = "You haven't reported any incidents yet. Tap the 'Report Incident' tab to create one.";
        ViewIncidentDetailsCommand = new Command<IncidentResponseDto>(async (incident) => await OnViewIncidentDetails(incident));

        // Load incidents when the ViewModel is created
        MainThread.BeginInvokeOnMainThread(async () => await LoadMyReports());
    }

    private async Task OnViewIncidentDetails(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={incident.Id}");
    }

    protected override async Task OnEditIncident(IncidentResponseDto incident)
    {
        if (incident == null) return;
        await Shell.Current.GoToAsync($"{nameof(EditIncidentPage)}?id={incident.Id}");
        await LoadMyReports(); // Refresh the list after editing
    }

    protected override async Task OnDeleteIncident(IncidentResponseDto incident)
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

                // Immediately remove the item from the collection
                if (Incidents.Contains(incident))
                {
                    Incidents.Remove(incident);
                }

                // Optionally refresh the list to ensure consistency
                await LoadMyReports();
            }
            catch (UnauthorizedAccessException)
            {
                await HandleUnauthorizedAccess();
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
    private async Task LoadMyReports()
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
                ErrorMessage = "Please log in to view your incidents.";
                Incidents.Clear(); // Clear the collection if not authenticated
                await HandleUnauthorizedAccess();
                return;
            }

            var incidents = await _incidentService.GetMyIncidentsAsync();

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
            });
        }
        catch (UnauthorizedAccessException)
        {
            await HandleUnauthorizedAccess();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MainThread.BeginInvokeOnMainThread(() => Incidents.Clear()); // Clear on error
        }
        finally
        {
            IsLoading = false;
        }
    }
}