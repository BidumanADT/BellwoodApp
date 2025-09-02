using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BellwoodGlobal.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IHttpClientFactory _factory;

    public MainPage(IHttpClientFactory factory)
    {
        InitializeComponent();
        _factory = factory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRidesAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadRidesAsync();
    }

    private async Task LoadRidesAsync()
    {
        try
        {
            ErrorLabel.IsVisible = false;
            RidesList.IsVisible = false;

            var token = Preferences.Get("access_token", null);
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorLabel.Text = "Please sign in first.";
                ErrorLabel.IsVisible = true;
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var rides = await GetRidesAsync(token);
            RidesList.ItemsSource = rides;
            RidesList.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }

    private async Task<Ride[]> GetRidesAsync(string token)
    {
        var ridesClient = _factory.CreateClient("rides");
        ridesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var data = await ridesClient.GetFromJsonAsync<Ride[]>("api/rides");
        return data ?? Array.Empty<Ride>();
    }

    public record Ride(DateTime Date, double Distance);
}
