using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _auth;

    public SplashPage(IAuthService auth, IConfigurationService config)
    {
        InitializeComponent();
        _auth = auth;
        // config is accepted for DI compatibility but no longer used here;
        // initialization is now performed eagerly in MauiProgram.CreateMauiApp().
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Splash animation while the UI renders.
            // Configuration is already loaded (MauiProgram), so no need to
            // await InitializeAsync() here.
            await AnimateSplashAsync();

#if DEBUG
            System.Diagnostics.Debug.WriteLine("[SplashPage] Splash complete, navigating...");
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
