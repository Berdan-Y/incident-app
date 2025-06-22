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
            if (Guid.TryParse(value, out Guid id))
            {
                MainThread.BeginInvokeOnMainThread(async () => await _viewModel.LoadIncident(id));
            }
            else
            {
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
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private void OnMapLoaded(object sender, EventArgs e)
    {
        if (sender is Microsoft.Maui.Controls.Maps.Map map &&
            BindingContext is EditIncidentViewModel viewModel &&
            viewModel.MapPins.Count > 0)
        {
            var pin = viewModel.MapPins[0];
            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                pin.Location,
                Distance.FromKilometers(1)
            ));
        }
    }
}