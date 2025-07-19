using System;                               // for TimeSpan, Func<…>, etc.
using System.Net.Http;                      // for HttpClientHandler, HttpMessageHandler
using Microsoft.Maui.Controls.Hosting;      // for UseMauiApp<T>
using Microsoft.Maui.Hosting;               // for MauiAppBuilder, CreateBuilder()
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityModel.Client;                  // for DiscoveryPolicy
using IdentityModel.OidcClient;              // for OidcClientOptions, Policy
using IdentityModel.OidcClient.Browser;      // for IBrowser, WebAuthenticatorBrowser


namespace BellwoodGlobal.Mobile
{
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

            builder.Services.AddSingleton(sp =>
            {
                var options = new OidcClientOptions
                {
                    Authority = "https://10.0.2.2:5001",
                    ClientId = "bellwood.passenger",
                    Scope = "openid profile ride.api offline_access",
                    RedirectUri = "com.bellwood.mobile://callback",
                    Browser = new WebAuthenticatorBrowser(),

                    Policy = new Policy
                    {
                        RequireIdentityTokenSignature = false,
                        Discovery = new DiscoveryPolicy
                        {
                            RequireHttps = true
                        }
                    },

                    BackchannelHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }
                };

                return new OidcClient(options);
            });

            builder.Services.AddTransient<MainPage>();
            return builder.Build();
        }
    }
}
