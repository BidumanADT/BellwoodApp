using IdentityModel.OidcClient;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls;  // for ContentPage, etc.

namespace BellwoodGlobal.Mobile 
{
    public partial class MainPage : ContentPage
    {
        private readonly OidcClient _oidcClient;

        public MainPage(OidcClient oidcClient)
        {
            InitializeComponent();   // wires up your XAML fields
            _oidcClient = oidcClient;
        }

        async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                LoginButton.IsEnabled = false;
                ResultLabel.Text = "Logging in…";

                var loginResult = await _oidcClient.LoginAsync(new LoginRequest());

                if (loginResult.IsError)
                {
                    ResultLabel.Text = $"Login error: {loginResult.Error}";
                    return;
                }

                var accessToken = loginResult.AccessToken;

                //using var client = new HttpClient();
                //client.DefaultRequestHeaders.Authorization =
                //    new AuthenticationHeaderValue("Bearer", accessToken);

                //var apiUrl = "https://localhost:7299/api/rides";
                //var apiUrl = "http://10.0.2.2:5042/api/rides";
                var apiHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new HttpClient(apiHandler);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // Hit the HTTPS endpoint on 10.0.2.2
                var apiUrl = "https://10.0.2.2:5042/api/rides";
                var responseJson = await client.GetStringAsync(apiUrl);

                ResultLabel.Text = responseJson;
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"Exception: {ex.Message}";
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}
