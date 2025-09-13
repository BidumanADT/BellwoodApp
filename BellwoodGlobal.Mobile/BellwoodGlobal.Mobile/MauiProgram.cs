using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // register services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddTransient<AuthHttpHandler>();

        // Auth Server client
        builder.Services.AddHttpClient("auth", c =>
        {
        #if ANDROID
            c.BaseAddress = new Uri("https://10.0.2.2:5001");
        #else
            c.BaseAddress = new Uri("https://localhost:5001");
        #endif
            c.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // dev certs
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // Rides API client
        builder.Services.AddHttpClient("rides", c =>
        {
        #if ANDROID
                    c.BaseAddress = new Uri("https://10.0.2.2:5005");
        #else
            c.BaseAddress = new Uri("https://localhost:5005");
        #endif
                    c.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                })
        .AddHttpMessageHandler<AuthHttpHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<MainPage>();
        
        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
