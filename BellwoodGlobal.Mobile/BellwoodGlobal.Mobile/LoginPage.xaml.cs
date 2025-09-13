using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile
{
    public partial class LoginPage : ContentPage
    {
        private readonly IHttpClientFactory _factory;
        private readonly IAuthService _auth;

        public LoginPage(IHttpClientFactory factory, IAuthService auth)
        {
            InitializeComponent();
            _factory = factory;
            _auth = auth;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            LoginButton.IsEnabled = false;
            ErrorLabel.IsVisible = false;

            try
            {
                var client = _factory.CreateClient("auth");
                var creds = new { Username = UsernameEntry.Text?.Trim(), Password = PasswordEntry.Text };

                var res = await client.PostAsJsonAsync("/login", creds);
                if (!res.IsSuccessStatusCode)
                {
                    ErrorLabel.Text = "Invalid username or password";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
                if (body is null || string.IsNullOrWhiteSpace(body.Token))
                {
                    ErrorLabel.Text = "Login failed: empty token";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                await _auth.SetTokenAsync(body.Token);

                // go to rides
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
