using Microsoft.Maui.Controls;
using Refit;
using Shared.Api;
using Shared.Models.Dtos;
using System.Diagnostics;

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
            if (string.IsNullOrEmpty(EmailEntry?.Text) || string.IsNullOrEmpty(PasswordEntry?.Text))
            {
                await DisplayAlert("Error", "Please enter both email and password", "OK");
                return;
            }

            var loginDto = new LoginDto
            {
                Email = EmailEntry.Text,
                Password = PasswordEntry.Text
            };

            try
            {
                Debug.WriteLine($"Attempting to login on platform: {DeviceInfo.Platform}");
                
                var httpClient = _authApi.GetType()
                    .GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_authApi) as HttpClient;
                
                if (httpClient != null)
                {
                    Debug.WriteLine($"API Client BaseAddress: {httpClient.BaseAddress}");
                }
                
                var response = await _authApi.LoginAsync(loginDto);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Login successful!", "OK");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await DisplayAlert("Error", "Login failed: Incorrect credentials", "OK");
                }
                else
                {
                    await DisplayAlert("Error", $"Login failed: {response.StatusCode} - {response.Content}", "OK");
                }
            }
            catch (ApiException apiEx)
            {
                Debug.WriteLine($"API Exception: {apiEx.Message}");
                Debug.WriteLine($"Status Code: {apiEx.StatusCode}");
                Debug.WriteLine($"Content: {apiEx.Content}");
                await DisplayAlert("Error", $"Login failed: {apiEx.StatusCode} - {apiEx.Content}", "OK");
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP Exception: {httpEx.Message}");
                Debug.WriteLine($"Inner Exception: {httpEx.InnerException?.Message}");
                await DisplayAlert("Error", $"Connection error: {httpEx.Message}", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Exception: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}