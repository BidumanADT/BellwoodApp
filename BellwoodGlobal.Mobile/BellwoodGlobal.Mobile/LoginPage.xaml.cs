using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BellwoodGlobal.Mobile
{
    public partial class LoginPage : ContentPage
    {
        private readonly IHttpClientFactory _factory;

        public LoginPage(IHttpClientFactory factory)
        {
            InitializeComponent();
            _factory = factory;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            LoginButton.IsEnabled = false;
            ErrorLabel.IsVisible = false;

            try
            {
                var auth = _factory.CreateClient("auth");

                var creds = new
                {
                    Username = UsernameEntry.Text?.Trim(),
                    Password = PasswordEntry.Text
                };

                var res = await auth.PostAsJsonAsync("/login", creds);
                if (!res.IsSuccessStatusCode)
                {
                    ErrorLabel.Text = "Invalid username or password";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
                if (body is null || string.IsNullOrEmpty(body.Token))
                {
                    ErrorLabel.Text = "Login failed: empty token";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                // Save token securely
                await SecureStorage.SetAsync("access_token", body.Token);


                //// Save token
                //Preferences.Set("access_token", body.Token);

                // Navigate to main rides page within the existing Shell
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private record LoginResponse(string Token);
    }
}
