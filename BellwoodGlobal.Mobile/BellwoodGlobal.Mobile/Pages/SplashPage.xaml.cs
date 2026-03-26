using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly IConfigurationService _config;

    public SplashPage(IAuthService auth, IConfigurationService config)
    {
        InitializeComponent();
        _auth = auth;
        _config = config;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Run config initialization and splash animation concurrently.
            // Config must be fully loaded before any HTTP client is used, so we
            // await both tasks together. The animation already takes ~800ms, which
            // is more than enough time for the file I/O to complete.
            await Task.WhenAll(
                _config.InitializeAsync(),
                AnimateSplashAsync()
            );

#if DEBUG
            System.Diagnostics.Debug.WriteLine("[SplashPage] Config initialization complete before navigation");
#endif

            // Create Shell as the new root
            Application.Current!.MainPage = new AppShell();

            // Do auth check
            var token = await _auth.GetValidTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                // route to LoginPage (registered in AppShell)
                await Shell.Current.GoToAsync(nameof(LoginPage));
            }
            else
            {
                // route to main dashboard 
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
        catch
        {
            // Worst-case: still show the app
            Application.Current!.MainPage = new AppShell();
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }

    private async Task AnimateSplashAsync()
    {
        // Defensive checks so we never NRE
        if (Logo is not null)
        {
            Logo.Opacity = 0;
            Logo.Scale = 0.8;
            await Logo.FadeTo(1, 400, Easing.CubicOut);
            await Logo.ScaleTo(1.0, 400, Easing.CubicOut);
        }

        // Small pause so splash is visible (minimum 800ms total)
        await Task.Delay(400);
    }
}
