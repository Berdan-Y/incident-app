using Maui.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Maui.Pages;

public partial class IncidentDetailsPage : ContentPage
{
    private readonly IncidentDetailsViewModel _viewModel;
    private Pin? _incidentPin;
    private bool _isMapInitialized;
    private bool _isPageLoaded;

    public IncidentDetailsPage(IncidentDetailsViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Subscribe to the PropertyChanged event of the ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.RegionChanged += OnRegionChanged;

        }
        catch (Exception ex)
        {
        }
    }

    private void InitializeMap()
    {
        try
        {
            if (LocationMap == null)
            {
                _isMapInitialized = false;
                return;
            }

            // Clear any existing pins
            if (LocationMap.Pins != null)
            {
                foreach (var pin in LocationMap.Pins.ToList())
                {
                    LocationMap.Pins.Remove(pin);
                }
            }

            LocationMap.MapType = MapType.Street;
            LocationMap.IsShowingUser = false;
            LocationMap.IsZoomEnabled = true;
            LocationMap.IsScrollEnabled = true;

            _isMapInitialized = true;
        }
        catch (Exception ex)
        {
            _isMapInitialized = false;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (!_isPageLoaded)
            {
                _isPageLoaded = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Initialize map first
                        if (LocationMap != null)
                        {
                            InitializeMap();
                        }

                        // Add a small delay to ensure the map is ready
                        await Task.Delay(500);

                        // Update pin if we have valid coordinates
                        if (_viewModel.HasValidCoordinates && _viewModel.Incident != null)
                        {
                            UpdateMapPin();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void OnRegionChanged(object? sender, MapSpan e)
    {
        try
        {
            if (LocationMap != null && _isMapInitialized)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        LocationMap.MoveToRegion(e);
                    }
                    catch (Exception ex)
                    {
                    }
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void UpdateMapPin()
    {
        try
        {
            if (!_viewModel.HasValidCoordinates || _viewModel.Incident == null || !_isMapInitialized || LocationMap == null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Remove existing pin if any
                    if (_incidentPin != null && LocationMap.Pins != null)
                    {
                        LocationMap.Pins.Remove(_incidentPin);
                        _incidentPin = null;
                    }

                    // Create new pin
                    var pinLocation = new Location(_viewModel.Incident.Latitude, _viewModel.Incident.Longitude);
                    _incidentPin = new Pin
                    {
                        Label = _viewModel.Incident.Title ?? "Incident Location",
                        Address = _viewModel.Incident.Address,
                        Type = PinType.Place,
                        Location = pinLocation
                    };


                    // Add pin to map
                    LocationMap.Pins?.Add(_incidentPin);

                    // Move to pin location
                    var mapSpan = MapSpan.FromCenterAndRadius(
                        pinLocation,
                        Distance.FromKilometers(0.25)
                    );
                    LocationMap.MoveToRegion(mapSpan);

                }
                catch (Exception ex)
                {
                }
            });
        }
        catch (Exception ex)
        {
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(IncidentDetailsViewModel.HasValidCoordinates) ||
                e.PropertyName == nameof(IncidentDetailsViewModel.Incident))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Initialize map if needed
                        if (!_isMapInitialized && LocationMap != null)
                        {
                            InitializeMap();
                            await Task.Delay(100); // Small delay after initialization
                        }

                        // Update pin if we have valid coordinates
                        if (_viewModel.HasValidCoordinates && _viewModel.Incident != null)
                        {
                            UpdateMapPin();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _viewModel.RegionChanged -= OnRegionChanged;
        _isPageLoaded = false;
        _isMapInitialized = false;

        if (_incidentPin != null && LocationMap?.Pins != null)
        {
            LocationMap.Pins.Remove(_incidentPin);
            _incidentPin = null;
        }
    }
}