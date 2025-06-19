using Maui.ViewModels;

namespace Maui.Pages;

public partial class AssignedIncidentsPage : ContentPage
{
    public AssignedIncidentsPage(AssignedIncidentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}