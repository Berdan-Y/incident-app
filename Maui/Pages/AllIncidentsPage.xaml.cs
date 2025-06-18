using Maui.ViewModels;

namespace Maui.Pages;

public partial class AllIncidentsPage : ContentPage
{
    public AllIncidentsPage(AllIncidentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 