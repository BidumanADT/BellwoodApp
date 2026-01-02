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
            .UseMauiMaps() // Enable MAUI Maps
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Montserrat-Regular.ttf", "Montserrat");
                fonts.AddFont("Montserrat-SemiBold.ttf", "MontserratSemibold");
                fonts.AddFont("PlayfairDisplay-SemiBold.ttf", "Playfair");
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
        builder.Services.AddTransient<QuoteDashboardPage>();
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<BookRidePage>();
        builder.Services.AddTransient<BookingsPage>();
        builder.Services.AddTransient<BookingDetailPage>();
        builder.Services.AddTransient<DriverTrackingPage>();
        builder.Services.AddTransient<PlacesTestPage>(); // Phase 1 test page
        builder.Services.AddTransient<LocationAutocompleteTestPage>(); // Phase 2 test page

        // Services
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>(); // PHASE 2: Secure config
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IRideService, RideService>();
        builder.Services.AddSingleton<IQuoteService, QuoteService>();
        builder.Services.AddSingleton<IProfileService, ProfileService>();
        builder.Services.AddSingleton<IQuoteDraftBuilder, QuoteDraftBuilder>();
        builder.Services.AddSingleton<ITripDraftBuilder, TripDraftBuilder>();
        builder.Services.AddSingleton<IPaymentService, PaymentService>();
        builder.Services.AddSingleton<ILocationPickerService, LocationPickerService>();
        builder.Services.AddSingleton<IDriverTrackingService, DriverTrackingService>();
        builder.Services.AddSingleton<IRideStatusService, RideStatusService>();
        builder.Services.AddSingleton<IPlacesAutocompleteService, PlacesAutocompleteService>();
        builder.Services.AddSingleton<IPlacesUsageTracker, PlacesUsageTracker>(); // NEW: Phase 7 usage tracking
        builder.Services.AddSingleton<IFormStateService, FormStateService>(); // Phase 5 form persistence

        // Auth handler for protected API calls
        builder.Services.AddTransient<AuthHttpHandler>();

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

        builder.Services.AddSingleton<IAdminApi, AdminApi>();

        // -------- HttpClients --------

        // Google Places API (New) client
        builder.Services.AddHttpClient("places", (serviceProvider, c) =>
        {
            c.BaseAddress = new Uri("https://places.googleapis.com/");
            c.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            // PHASE 2: Get API key from secure configuration service
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            var apiKey = configService.GetPlacesApiKey();
            c.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
            
#if ANDROID
            // PHASE 1: Add Android-specific headers for API key restrictions
            // These headers allow Google to verify the app's identity
            try
            {
                var packageName = Platforms.Android.AndroidPackageHelper.GetPackageName();
                var certFingerprint = Platforms.Android.AndroidPackageHelper.GetCertificateFingerprint();
                
                c.DefaultRequestHeaders.Add("X-Android-Package", packageName);
                c.DefaultRequestHeaders.Add("X-Android-Cert", certFingerprint);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] Android Package: {packageName}");
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] Android Cert: {certFingerprint}");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] WARNING: Could not get Android headers: {ex.Message}");
#endif
                // Continue without headers - will work if API key has no restrictions
                // In production, this should be logged to error tracking
            }
#endif
            
            c.Timeout = TimeSpan.FromSeconds(10);
        });

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
