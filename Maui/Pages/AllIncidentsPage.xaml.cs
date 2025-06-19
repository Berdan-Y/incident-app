using Maui.ViewModels;

namespace Maui.Pages;

public partial class AllIncidentsPage : ContentPage
{
    private readonly AllIncidentsViewModel _viewModel;
    
    public AllIncidentsPage(AllIncidentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadIncidentsCommand.Execute(null);
    }
} 