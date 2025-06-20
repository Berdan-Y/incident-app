using Maui.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;

namespace Maui.Pages;

[QueryProperty(nameof(IncidentId), "id")]
public partial class EditIncidentPage : ContentPage
{
    private readonly EditIncidentViewModel _viewModel;

    public string IncidentId
    {
        set
        {
            Debug.WriteLine($"Setting IncidentId: {value}");
            if (Guid.TryParse(value, out Guid id))
            {
                Debug.WriteLine($"Successfully parsed IncidentId: {id}");
                MainThread.BeginInvokeOnMainThread(async () => await _viewModel.LoadIncident(id));
            }
            else
            {
                Debug.WriteLine($"Failed to parse IncidentId: {value}");
                MainThread.BeginInvokeOnMainThread(async () => 
                {
                    await Shell.Current.DisplayAlert("Error", "Invalid incident ID", "OK");
                    await Shell.Current.GoToAsync("..");
                });
            }
        }
    }

    public EditIncidentPage(EditIncidentViewModel viewModel)
    {
        Debug.WriteLine("Initializing EditIncidentPage");
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Debug.WriteLine("EditIncidentPage initialized with ViewModel");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("EditIncidentPage OnAppearing");
        Debug.WriteLine($"BindingContext is {(BindingContext != null ? "set" : "null")}");
        Debug.WriteLine($"ValidateAddressCommand is {(_viewModel.ValidateAddressCommand != null ? "set" : "null")}");
        Debug.WriteLine($"SaveCommand is {(_viewModel.SaveCommand != null ? "set" : "null")}");
    }

    private void OnMapLoaded(object sender, EventArgs e)
    {
        Debug.WriteLine("Map loaded");
        if (sender is Microsoft.Maui.Controls.Maps.Map map && 
            BindingContext is EditIncidentViewModel viewModel && 
            viewModel.MapPins.Count > 0)
        {
            var pin = viewModel.MapPins[0];
            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                pin.Location,
                Distance.FromKilometers(1)
            ));
            Debug.WriteLine("Map region updated");
        }
    }
}