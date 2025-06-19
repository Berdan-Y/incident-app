using Maui.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Maui.Pages;

public partial class IncidentDetailsPage : ContentPage
{
    private readonly IncidentDetailsViewModel _viewModel;
    private Pin _incidentPin;

    public IncidentDetailsPage(IncidentDetailsViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Initializing IncidentDetailsPage");
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Subscribe to the PropertyChanged event of the ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            System.Diagnostics.Debug.WriteLine("Added property changed handler");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in IncidentDetailsPage constructor: {ex}");
        }
    }

    private void UpdateMapPin()
    {
        if (_viewModel.HasValidCoordinates && _viewModel.Incident != null)
        {
            // Remove existing pin if any
            if (_incidentPin != null)
            {
                LocationMap.Pins.Remove(_incidentPin);
            }

            // Create new pin
            var position = new Location(_viewModel.Incident.Latitude, _viewModel.Incident.Longitude);
            _incidentPin = new Pin
            {
                Label = "Incident Location",
                Address = _viewModel.Incident.Address,
                Type = PinType.Place,
                Location = position
            };

            // Add pin and move map
            LocationMap.Pins.Add(_incidentPin);
            LocationMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                position,
                Distance.FromMeters(100) // Zoom to 100 meters for precise location
            ));

            System.Diagnostics.Debug.WriteLine($"Updated pin location: {position.Latitude:F6}, {position.Longitude:F6}");
        }
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(IncidentDetailsViewModel.HasValidCoordinates) ||
                e.PropertyName == nameof(IncidentDetailsViewModel.Incident))
            {
                MainThread.BeginInvokeOnMainThread(UpdateMapPin);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ViewModel_PropertyChanged: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("IncidentDetailsPage OnDisappearing");
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (_incidentPin != null)
        {
            LocationMap.Pins.Remove(_incidentPin);
            _incidentPin = null;
        }
    }
}