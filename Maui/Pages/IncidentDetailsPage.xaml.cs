using Maui.ViewModels;

namespace Maui.Pages;

public partial class IncidentDetailsPage : ContentPage
{
    public IncidentDetailsPage(IncidentDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 