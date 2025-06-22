using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Maui.Pages;
using Shared.Api;
using Shared.Models.Dtos;

namespace Maui.ViewModels;

public partial class NotificationsViewModel : BaseViewModel
{
    private readonly INotificationApi _notificationApi;

    [ObservableProperty]
    private ObservableCollection<NotificationDto> _notifications = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _hasUnreadNotifications;

    public ICommand LoadNotificationsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NotificationTappedCommand { get; }
    public ICommand ToggleReadStatusCommand { get; }
    public ICommand MarkAllAsReadCommand { get; }

    public NotificationsViewModel(INotificationApi notificationApi)
    {
        _notificationApi = notificationApi;

        LoadNotificationsCommand = new AsyncRelayCommand(LoadNotificationsAsync);
        RefreshCommand = new AsyncRelayCommand(LoadNotificationsAsync);
        NotificationTappedCommand = new AsyncRelayCommand<NotificationDto>(OnNotificationTappedAsync);
        ToggleReadStatusCommand = new AsyncRelayCommand<NotificationDto>(ToggleReadStatusAsync);
        MarkAllAsReadCommand = new AsyncRelayCommand(MarkAllAsReadAsync);
    }

    private async Task LoadNotificationsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await _notificationApi.GetNotificationsAsync();
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                Notifications.Clear();
                foreach (var notification in response.Content)
                {
                    Notifications.Add(notification);
                }
                HasUnreadNotifications = Notifications.Any(n => !n.IsRead);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task OnNotificationTappedAsync(NotificationDto notification)
    {
        if (notification == null)
            return;

        try
        {
            // Mark as read if not already read
            if (!notification.IsRead)
            {
                await MarkAsReadAsync(notification);
            }

            // Navigate to the incident
            await Shell.Current.GoToAsync($"{nameof(IncidentDetailsPage)}?id={notification.IncidentId}");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ToggleReadStatusAsync(NotificationDto notification)
    {
        if (notification == null)
            return;

        try
        {
            var response = notification.IsRead
                ? await _notificationApi.MarkAsUnreadAsync(notification.Id)
                : await _notificationApi.MarkAsReadAsync(notification.Id);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var index = Notifications.IndexOf(notification);
                if (index != -1)
                {
                    Notifications[index] = response.Content;
                }
                HasUnreadNotifications = Notifications.Any(n => !n.IsRead);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task MarkAsReadAsync(NotificationDto notification)
    {
        try
        {
            var response = await _notificationApi.MarkAsReadAsync(notification.Id);
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var index = Notifications.IndexOf(notification);
                if (index != -1)
                {
                    Notifications[index] = response.Content;
                }
                HasUnreadNotifications = Notifications.Any(n => !n.IsRead);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task MarkAllAsReadAsync()
    {
        try
        {
            var response = await _notificationApi.MarkAllAsReadAsync();
            if (response.IsSuccessStatusCode)
            {
                await LoadNotificationsAsync();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}