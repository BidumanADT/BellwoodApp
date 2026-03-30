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

        // ── Early configuration initialization ──────────────────────────
        // ConfigurationService MUST be fully loaded before builder.Build()
        // because HttpClient factory delegates (admin, places, auth) call
        // Get*() methods at client-creation time.  If any singleton that
        // depends on IHttpClientFactory is resolved during Build(), those
        // delegates fire immediately — and they will throw
        // InvalidOperationException if config is still uninitialised.
        // Blocking here is intentional: the cost is ~250 ms of file I/O
        // and it only runs once, before any UI is shown.
        var configService = new ConfigurationService();
        configService.InitializeAsync().GetAwaiter().GetResult();
        builder.Services.AddSingleton<IConfigurationService>(configService);

        // Pages (DI-friendly even if we now use parameterless ctors)
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddTransient<MainPage>();
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
        // NOTE: IConfigurationService already registered above — do NOT re-register.
        builder.Services.AddSingleton<IAuthService, AuthService>();
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

        builder.Services.AddHttpClient("admin", (serviceProvider, c) =>
        {
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            
#if ANDROID
            // Android emulator uses 10.0.2.2 to reach host machine
            // In production, this should be the actual API URL
            var baseUrl = configService.GetAdminApiUrl().Replace("localhost", "10.0.2.2");
            c.BaseAddress = new Uri(baseUrl);
#else
            c.BaseAddress = new Uri(configService.GetAdminApiUrl());
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
            
            // PHASE 2: Get platform-specific API key from secure configuration service
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();

#if ANDROID
            var apiKey = configService.GetPlacesApiKey(); // Android-restricted key
#elif IOS || MACCATALYST
            var apiKey = configService.GetPlacesApiKeyIos(); // iOS-restricted key
#else
            var apiKey = configService.GetPlacesApiKey(); // Fallback (Windows, etc.)
#endif

            c.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
            
#if ANDROID
            // Add Android-specific headers for Google Places API key restrictions
            try
            {
                var packageName = Platforms.Android.AndroidPackageHelper.GetPackageName();
                var certFingerprint = Platforms.Android.AndroidPackageHelper.GetCertificateFingerprint();

                c.DefaultRequestHeaders.Add("X-Android-Package", packageName);
                c.DefaultRequestHeaders.Add("X-Android-Cert", certFingerprint);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] Android Package: {packageName}");
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] Android Cert: {certFingerprint}");
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] Using ANDROID Places API key");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlacesAPI] WARNING: Could not get Android headers: {ex.Message}");
#endif
                // Continue without headers - will work if API key has no restrictions
            }
#elif IOS || MACCATALYST
            // Add iOS/Mac Catalyst bundle ID header for Google Places API key restrictions
            c.DefaultRequestHeaders.Add("X-Ios-Bundle-Identifier", "com.bellwoodglobal.mobile");
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[PlacesAPI] iOS bundle ID header added: com.bellwoodglobal.mobile");
            System.Diagnostics.Debug.WriteLine("[PlacesAPI] Using iOS Places API key");
#endif
#endif
            
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        // Auth Server client
        builder.Services.AddHttpClient("auth", (serviceProvider, c) =>
        {
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            
#if ANDROID
            var baseUrl = configService.GetAuthServerUrl().Replace("localhost", "10.0.2.2");
            c.BaseAddress = new Uri(baseUrl);
#else
            c.BaseAddress = new Uri(configService.GetAuthServerUrl());
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

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
