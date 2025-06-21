using Maui.ViewModels;

namespace Maui.Pages;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is NotificationsViewModel viewModel)
        {
            viewModel.LoadNotificationsCommand.Execute(null);
        }
    }
} 