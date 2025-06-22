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
            System.Diagnostics.Debug.WriteLine("Initializing IncidentDetailsPage");
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Subscribe to the PropertyChanged event of the ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.RegionChanged += OnRegionChanged;
            
            System.Diagnostics.Debug.WriteLine("Added property changed handler");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in IncidentDetailsPage constructor: {ex}");
        }
    }

    private void InitializeMap()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Initializing map");
            if (LocationMap == null)
            {
                System.Diagnostics.Debug.WriteLine("LocationMap is null, cannot initialize");
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
            System.Diagnostics.Debug.WriteLine("Map initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in InitializeMap: {ex}");
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
                        System.Diagnostics.Debug.WriteLine($"Exception in OnAppearing async lambda: {ex}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in OnAppearing: {ex}");
        }
    }

    private void OnRegionChanged(object? sender, MapSpan e)
    {
        try
        {
            if (LocationMap != null && _isMapInitialized)
            {
                System.Diagnostics.Debug.WriteLine($"Moving map to new region: Center({e.Center.Latitude}, {e.Center.Longitude}), Radius: {e.Radius.Kilometers}km");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        LocationMap.MoveToRegion(e);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Exception moving map region: {ex}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in OnRegionChanged: {ex}");
        }
    }

    private void UpdateMapPin()
    {
        try
        {
            if (!_viewModel.HasValidCoordinates || _viewModel.Incident == null || !_isMapInitialized || LocationMap == null)
            {
                System.Diagnostics.Debug.WriteLine("Cannot update pin - prerequisites not met");
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

                    System.Diagnostics.Debug.WriteLine($"Adding pin at {pinLocation.Latitude}, {pinLocation.Longitude} with label '{_incidentPin.Label}'");

                    // Add pin to map
                    LocationMap.Pins?.Add(_incidentPin);

                    // Move to pin location
                    var mapSpan = MapSpan.FromCenterAndRadius(
                        pinLocation,
                        Distance.FromKilometers(0.25)
                    );
                    LocationMap.MoveToRegion(mapSpan);

                    System.Diagnostics.Debug.WriteLine("Pin added and map centered");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception in UpdateMapPin inner lambda: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in UpdateMapPin: {ex}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Property changed: {e.PropertyName}");
            
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
                        System.Diagnostics.Debug.WriteLine($"Exception in PropertyChanged async lambda: {ex}");
                    }
                });
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