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

            // If RequireSignInAsync navigates, bail out gracefully
            await _auth.RequireSignInAsync();
            if (!await _auth.IsSignedInAsync()) return;

            var rides = await GetRidesAsyncDetailed();
            RidesList.ItemsSource = rides;
            RidesList.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"ERR: {ex.GetType().Name}: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async Task<Ride[]> GetRidesAsyncDetailed()
    {
        var client = _factory.CreateClient("rides"); // AuthHttpHandler should add Bearer

        using var req = new HttpRequestMessage(HttpMethod.Get, "api/rides");
        using var res = await client.SendAsync(req);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            // Show precise HTTP info (temporary)
            throw new InvalidOperationException(
                $"HTTP {(int)res.StatusCode} {res.StatusCode}. Body: {Truncate(body, 300)}");
        }

        var stream = await res.Content.ReadAsStreamAsync();
        var data = await System.Text.Json.JsonSerializer.DeserializeAsync<Ride[]>(
            stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return data ?? Array.Empty<Ride>();

        static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _auth.LogoutAsync();
    }

    public record Ride(DateTime Date, double Distance);
}
