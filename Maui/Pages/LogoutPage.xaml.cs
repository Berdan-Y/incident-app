using Maui.ViewModels;

namespace Maui.Pages;

public partial class LogoutPage : ContentPage
{
    public LogoutPage(LogoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}