using Microsoft.Maui.Controls;
using Refit;
using Shared.Api;
using Shared.Models.Dtos;

namespace Maui.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly IAuthApi _authApi;

        public LoginPage(IAuthApi authApi)
        {
            InitializeComponent();
            _authApi = authApi;
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            var email = UsernameEntry.Text;
            var password = PasswordEntry.Text;

            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            try
            {
                var response = await _authApi.LoginAsync(loginDto);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", $"Login successful! Token: {response.Content}", "OK");
                }
                else
                {
                    await DisplayAlert("Error", $"Login failed: {response.StatusCode} - {response.Error?.Message}", "OK");
                }
            }
            catch (ApiException apiEx)
            {
                await DisplayAlert("Error", $"Login failed: {apiEx.StatusCode} - {apiEx.Content}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
} 