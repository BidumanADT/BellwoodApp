using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _auth;

    public SplashPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Defensive checks so we never NRE
            if (Logo is not null)
            {
                Logo.Opacity = 0;
                Logo.Scale = 0.8;
                await Logo.FadeTo(1, 400, Easing.CubicOut);
                await Logo.ScaleTo(1.0, 400, Easing.CubicOut);
            }

            // Create Shell as the new root
            Application.Current!.MainPage = new AppShell();

            // Small pause so splash is visible
            await Task.Delay(400);

            // Do auth check AFTER Shell is root
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
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
