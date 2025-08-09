using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BellwoodGlobal.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Auth Server client
        builder.Services.AddHttpClient("auth", c => {
            c.BaseAddress = new Uri("https://10.0.2.2:5001");
            c.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
              HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // Rides API client
        builder.Services.AddHttpClient("rides", c => {
            c.BaseAddress = new Uri("https://10.0.2.2:5005");
            c.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
              HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
