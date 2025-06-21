using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models.Dtos;
using Shared.Models.Enums;
using System.Diagnostics;

namespace Maui.ViewModels;

public partial class FieldEmployeeEditViewModel : ObservableObject
{
    private readonly IIncidentService _incidentService;
    private Guid _incidentId;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string description;

    [ObservableProperty]
    private string address;

    [ObservableProperty]
    private string zipCode;

    [ObservableProperty]
    private Status status;

    public ObservableCollection<Status> StatusOptions { get; } = new(Enum.GetValues<Status>());

    public FieldEmployeeEditViewModel()
    {
        _incidentService = Application.Current.Handler.MauiContext.Services.GetRequiredService<IIncidentService>();
    }

    public async Task LoadIncidentAsync(Guid incidentId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            _incidentId = incidentId;

            var incident = await _incidentService.GetIncidentByIdAsync(incidentId);
            
            Title = incident.Title;
            Description = incident.Description;
            Address = incident.Address;
            ZipCode = incident.ZipCode;
            Status = incident.Status;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load incident: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            Debug.WriteLine($"Saving incident {_incidentId} with Description: {Description}, Status: {Status}");

            // Only update status since field employees don't have permission to update other details
            await _incidentService.UpdateIncidentStatusAsync(_incidentId, (int)Status);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving incident: {ex}");
            ErrorMessage = $"Failed to save changes: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
} 