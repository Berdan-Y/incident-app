using Maui.ViewModels;

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
        }
    }

    public EditIncidentPage(EditIncidentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}