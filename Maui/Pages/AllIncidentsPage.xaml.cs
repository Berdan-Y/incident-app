using Maui.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Maui.Pages;

public partial class AllIncidentsPage : ContentPage
{
    private readonly AllIncidentsViewModel _viewModel;
    private bool _isMapInitialized;
    private bool _isPageLoaded;

    public AllIncidentsPage(AllIncidentsViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Subscribe to the PropertyChanged event of the ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in AllIncidentsPage constructor: {ex}");
        }
    }

    private void InitializeMap()
    {
        try
        {
            if (IncidentsMap == null)
            {
                _isMapInitialized = false;
                return;
            }

            // Clear any existing pins
            if (IncidentsMap.Pins != null)
            {
                foreach (var pin in IncidentsMap.Pins.ToList())
                {
                    IncidentsMap.Pins.Remove(pin);
                }
            }

            IncidentsMap.MapType = MapType.Street;
            IncidentsMap.IsShowingUser = true;
            IncidentsMap.IsZoomEnabled = true;
            IncidentsMap.IsScrollEnabled = true;

            _isMapInitialized = true;
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
                        if (IncidentsMap != null)
                        {
                            InitializeMap();
                        }

                        // Add a small delay to ensure the map is ready
                        await Task.Delay(500);

                        // Update pins if we have incidents
                        if (_viewModel.Incidents.Any())
                        {
                            UpdateMapPins();
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

    private void UpdateMapPins()
    {
        try
        {
            if (!_isMapInitialized || IncidentsMap == null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Clear existing pins
                    if (IncidentsMap.Pins != null)
                    {
                        IncidentsMap.Pins.Clear();
                    }

                    var validLocations = new List<Location>();

                    // Add pins for each incident with valid coordinates
                    foreach (var incident in _viewModel.Incidents)
                    {
                        if (incident.Latitude != 0 && incident.Longitude != 0)
                        {
                            var pinLocation = new Location(incident.Latitude, incident.Longitude);
                            validLocations.Add(pinLocation);

                            var pin = new Pin
                            {
                                Label = incident.Title ?? "Incident Location",
                                Address = incident.Address,
                                Type = PinType.Place,
                                Location = pinLocation
                            };

                            pin.MarkerClicked += async (s, e) =>
                            {
                                await _viewModel.OnViewIncidentDetails(incident);
                            };

                            IncidentsMap.Pins?.Add(pin);
                        }
                    }

                    // If we have valid locations, calculate the best region to show all pins
                    if (validLocations.Any())
                    {
                        var center = new Location(
                            validLocations.Average(l => l.Latitude),
                            validLocations.Average(l => l.Longitude)
                        );

                        // Calculate the distance needed to show all pins
                        var maxLat = validLocations.Max(l => l.Latitude);
                        var minLat = validLocations.Min(l => l.Latitude);
                        var maxLon = validLocations.Max(l => l.Longitude);
                        var minLon = validLocations.Min(l => l.Longitude);

                        var latDistance = maxLat - minLat;
                        var lonDistance = maxLon - minLon;
                        var radius = Math.Max(latDistance, lonDistance) * 111; // Convert to kilometers (roughly)

                        var mapSpan = MapSpan.FromCenterAndRadius(
                            center,
                            Distance.FromKilometers(radius + 1) // Add 1km for padding
                        );
                        IncidentsMap.MoveToRegion(mapSpan);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception in UpdateMapPins inner lambda: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in UpdateMapPins: {ex}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(AllIncidentsViewModel.IsMapView) && _viewModel.IsMapView)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        // Initialize map if needed
                        if (!_isMapInitialized && IncidentsMap != null)
                        {
                            InitializeMap();
                            await Task.Delay(100); // Small delay after initialization
                        }

                        // Update pins
                        UpdateMapPins();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Exception in PropertyChanged async lambda: {ex}");
                    }
                });
            }
            else if (e.PropertyName == nameof(AllIncidentsViewModel.Incidents))
            {
                if (_viewModel.IsMapView)
                {
                    UpdateMapPins();
                }
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
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _isPageLoaded = false;
        _isMapInitialized = false;

        if (IncidentsMap?.Pins != null)
        {
            IncidentsMap.Pins.Clear();
        }
    }
}