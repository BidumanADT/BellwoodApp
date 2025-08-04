using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls;

namespace BellwoodGlobal.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IHttpClientFactory _factory;

    public MainPage(IHttpClientFactory factory)
    {
        InitializeComponent();
        _factory = factory;
    }

    async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;
        ErrorLabel.IsVisible = false;
        RidesList.IsVisible = false;

        try
        {
            // 1) Call /login
            var auth = _factory.CreateClient("auth");
            var creds = new { Username = UsernameEntry.Text, Password = PasswordEntry.Text };
            var res = await auth.PostAsJsonAsync("/login", creds);
            if (!res.IsSuccessStatusCode)
                throw new Exception("Login failed");

            var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
            if (body?.token is not string token)
                throw new Exception("No token received");

            // 2) Call rides API
            var ridesClient = _factory.CreateClient("rides");
            ridesClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var rides = await ridesClient.GetFromJsonAsync<Ride[]>("api/rides");
            RidesList.ItemsSource = rides;
            RidesList.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    record LoginResponse(string token);
    public record Ride(DateTime Date, double Distance);
}
