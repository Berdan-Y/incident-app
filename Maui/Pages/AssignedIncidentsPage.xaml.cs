using Maui.ViewModels;

namespace Maui.Pages;

public partial class AssignedIncidentsPage : ContentPage
{
    private readonly AssignedIncidentsViewModel _viewModel;
    
    public AssignedIncidentsPage(AssignedIncidentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadIncidentsCommand.ExecuteAsync(null);
    }
}