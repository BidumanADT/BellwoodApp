using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BellwoodGlobal.Mobile.Pages;
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

        // Pages (DI-friendly even if we now use parameterless ctors)
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<RideHistoryPage>();
        builder.Services.AddTransient<QuotePage>();

        // Services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IRideService, RideService>();
        builder.Services.AddSingleton<IQuoteService, QuoteService>();
        builder.Services.AddSingleton<IProfileService, ProfileService>();
        builder.Services.AddSingleton<IQuoteDraftBuilder, QuoteDraftBuilder>();

        builder.Services.AddHttpClient("admin", c =>
        {
        #if ANDROID
                    c.BaseAddress = new Uri("https://10.0.2.2:5206");
        #else
            c.BaseAddress = new Uri("https://localhost:5206");
        #endif
                    c.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                })
        #if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // DEV ONLY: trust local dev certs
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
#else
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
#endif

        builder.Services.AddSingleton<IAdminApi, AdminApi>();

        // Auth handler for protected API calls
        builder.Services.AddTransient<AuthHttpHandler>();

        // -------- HttpClients --------

        // Auth Server client
        builder.Services.AddHttpClient("auth", c =>
        {
#if ANDROID
            c.BaseAddress = new Uri("https://10.0.2.2:5001");
#else
            c.BaseAddress = new Uri("https://localhost:5001");
#endif
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // DEV ONLY: trust local dev certs
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
#else
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
#endif

        // Rides API client (protected)
        builder.Services.AddHttpClient("rides", c =>
        {
#if ANDROID
            c.BaseAddress = new Uri("https://10.0.2.2:5005");
#else
            c.BaseAddress = new Uri("https://localhost:5005");
#endif
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<AuthHttpHandler>()
#if DEBUG
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // DEV ONLY: trust local dev certs
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
#else
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
