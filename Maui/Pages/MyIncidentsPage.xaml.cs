using Maui.ViewModels;
using Shared.Models.Dtos;
using System.Diagnostics;

namespace Maui.Pages;

public partial class MyIncidentsPage : ContentPage
{
    private MyIncidentsViewModel _viewModel;
    
    public MyIncidentsPage(MyIncidentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        Debug.WriteLine("MyIncidentsPage initialized");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("MyIncidentsPage OnAppearing called");
        await _viewModel.LoadMyReportsAsync();
        
        // Log the number of incidents loaded
        Debug.WriteLine($"Loaded {_viewModel.Incidents?.Count ?? 0} incidents");
        
        if (_viewModel.Incidents?.Count > 0)
        {
            foreach (var incident in _viewModel.Incidents)
            {
                Debug.WriteLine($"Incident available: {incident.Id} - {incident.Title}");
            }
        }
    }
    
    private async void OnIncidentSelected(object sender, SelectionChangedEventArgs e)
    {
        Debug.WriteLine("OnIncidentSelected called");
        Debug.WriteLine($"Number of current selections: {e.CurrentSelection?.Count ?? 0}");
        Debug.WriteLine($"Sender type: {sender?.GetType().Name}");
        
        if (e.CurrentSelection.FirstOrDefault() is IncidentResponseDto selectedIncident)
        {
            Debug.WriteLine($"Selected incident: {selectedIncident.Id} - {selectedIncident.Title}");
            
            try
            {
                _viewModel.ViewIncidentDetailsCommand.Execute(selectedIncident);
                Debug.WriteLine("Navigation command executed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to incident details: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", "Could not open incident details. Please try again.", "OK");
            }
        }
        else
        {
            Debug.WriteLine("No incident selected or invalid selection type");
            if (e.CurrentSelection?.FirstOrDefault() != null)
            {
                Debug.WriteLine($"Selected item type: {e.CurrentSelection.FirstOrDefault().GetType().Name}");
            }
        }

        // Clear the selection
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }
} 