using Maui.ViewModels;

namespace Maui.Pages;

[QueryProperty(nameof(IncidentId), "id")]
public partial class FieldEmployeeEditPage : ContentPage
{
    private string incidentId;
    public string IncidentId
    {
        get => incidentId;
        set
        {
            incidentId = value;
            LoadIncident();
        }
    }

    public FieldEmployeeEditPage()
    {
        InitializeComponent();
    }

    private async void LoadIncident()
    {
        if (BindingContext is FieldEmployeeEditViewModel viewModel)
        {
            await viewModel.LoadIncidentAsync(Guid.Parse(IncidentId));
        }
    }
} 