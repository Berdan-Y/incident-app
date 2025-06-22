using Maui.ViewModels;
using Shared.Models.Dtos;
using System.Diagnostics;

namespace Maui.Pages;

public partial class MyIncidentsPage : ContentPage
{
    private readonly MyIncidentsViewModel _viewModel;

    public MyIncidentsPage(MyIncidentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadMyReportsCommand.Execute(null);
    }
}