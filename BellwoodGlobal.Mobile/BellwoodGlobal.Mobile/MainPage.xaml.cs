using System.Net.Http.Json;
using System.Net.Http.Headers;

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
        ErrorLabel.IsVisible = false;
        RidesList.IsVisible = false;

        var authClient = _factory.CreateClient("auth");
        var loginReq = new { Username = UsernameEntry.Text, Password = PasswordEntry.Text };

        HttpResponseMessage loginResp;
        try
        {
            loginResp = await authClient.PostAsJsonAsync("login", loginReq);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Login failed: {ex.Message}";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!loginResp.IsSuccessStatusCode)
        {
            ErrorLabel.Text = "Invalid credentials";
            ErrorLabel.IsVisible = true;
            return;
        }

        var obj = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        var token = obj!.Token;

        // 3) Call Rides API
        var ridesClient = _factory.CreateClient("rides");
        ridesClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var rides = await ridesClient.GetFromJsonAsync<List<Ride>>();
            RidesList.ItemsSource = rides;
            RidesList.IsVisible = true;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Failed to load rides: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    // these models mirror the server:
    class TokenResponse { public string Token { get; set; } = ""; }
    class Ride { public DateTime Date { get; set; } public double Distance { get; set; } }
}
