using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IHttpClientFactory _factory;
    private readonly IAuthService _auth;

    public MainPage(IHttpClientFactory factory, IAuthService auth)
    {
        InitializeComponent();
        _factory = factory;
        _auth = auth;
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

    private async Task<Ride[]> GetRidesAsync()
    {
        var ridesClient = _factory.CreateClient("rides");
        // No header set here — handler injects it
        var data = await ridesClient.GetFromJsonAsync<Ride[]>("api/rides");
        return data ?? Array.Empty<Ride>();
    }

    private async Task LoadRidesAsync()
    {
        try
        {
            ErrorLabel.IsVisible = false;
            RidesList.IsVisible = false;

            await _auth.RequireSignInAsync();
            if (!await _auth.IsSignedInAsync()) return; // navigated to Login

            var rides = await GetRidesAsync();
            RidesList.ItemsSource = rides;
            RidesList.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _auth.LogoutAsync();
    }

    public record Ride(DateTime Date, double Distance);
}
