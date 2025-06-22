using System.ComponentModel;
using Microsoft.Maui.Controls.Maps;
using Maui.ViewModels;

namespace Maui.Pages;

public partial class ReportIncidentPage : ContentPage
{
    private readonly ReportIncidentViewModel _viewModel;

    public ReportIncidentPage(ReportIncidentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += ViewModelPropertyChanged;
    }

    private void OnMapLoaded(object sender, EventArgs e)
    {
        if (_viewModel.InitialMapPosition != null)
        {
            LocationMap.MoveToRegion(_viewModel.InitialMapPosition);
        }
    }

    private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.InitialMapPosition))
        {
            var mapSpan = _viewModel.InitialMapPosition;
            if (mapSpan != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LocationMap.MoveToRegion(mapSpan);
                });
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= ViewModelPropertyChanged;
    }
}