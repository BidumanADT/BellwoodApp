using System;
using System.Net.Http.Headers;    // for AuthenticationHeaderValue
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Maui.Controls;     // for ContentPage, etc.
using System.Net.Http;             // for IHttpClientFactory

namespace BellwoodGlobal.Mobile;

public partial class MainPage : ContentPage
{
    private readonly OidcClient _oidcClient;
    private readonly IHttpClientFactory _httpFactory;

    public MainPage(OidcClient oidcClient, IHttpClientFactory httpFactory)
    {
        InitializeComponent();
        _oidcClient = oidcClient;
        _httpFactory = httpFactory;
    }

    async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;
        ResultLabel.Text = "Logging in…";

        var loginResult = await _oidcClient.LoginAsync(new LoginRequest());
        if (loginResult.IsError)
        {
            ResultLabel.Text = $"Login error: {loginResult.Error}";
            LoginButton.IsEnabled = true;
            return;
        }

        // get a configured HttpClient
        var client = _httpFactory.CreateClient("rides");
        client.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        try
        {
            var ridesJson = await client.GetStringAsync("api/rides");
            ResultLabel.Text = ridesJson;
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"API call failed: {ex.Message}";
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
