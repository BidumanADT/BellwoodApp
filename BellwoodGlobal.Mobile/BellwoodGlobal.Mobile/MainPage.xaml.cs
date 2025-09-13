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

    private async Task LoadRidesAsync()
    {
        try
        {
            ErrorLabel.IsVisible = false;
            RidesList.IsVisible = false;

            await _auth.RequireSignInAsync();
            var token = await _auth.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) return; // navigated to Login

            var client = _factory.CreateClient("rides");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var data = await client.GetFromJsonAsync<Ride[]>("api/rides");
            RidesList.ItemsSource = data ?? Array.Empty<Ride>();
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
