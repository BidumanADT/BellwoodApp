using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // 1) HttpClient for AuthServer
        builder.Services.AddHttpClient("auth", client =>
        {
            client.BaseAddress = new Uri("https://10.0.2.2:5001/");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // for emulator → localhost
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // 2) HttpClient for RidesApi
        builder.Services.AddHttpClient("rides", client =>
        {
            client.BaseAddress = new Uri("https://10.0.2.2:5005/");
            client.DefaultRequestHeaders.Accept.Add(
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
