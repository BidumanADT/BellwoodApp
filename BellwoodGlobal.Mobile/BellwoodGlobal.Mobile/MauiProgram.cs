using System;
using System.Net.Http;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

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
                // 1. Create your OidcClientOptions
                var options = new OidcClientOptions
                {
                    Authority = "https://10.0.2.2:5001",
                    ClientId = "bellwood.passenger",
                    Scope = "openid profile ride.api offline_access",
                    RedirectUri = "com.bellwoodglobal.mobile://callback",
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

                // 2. Disable PAR and force the classic authorize‐token endpoints
                options.ProviderInformation = new ProviderInformation
                {
                    IssuerName = options.Authority,
                    AuthorizeEndpoint = $"{options.Authority}/connect/authorize",
                    TokenEndpoint = $"{options.Authority}/connect/token",
                    EndSessionEndpoint = $"{options.Authority}/connect/endsession",
                    UserInfoEndpoint = $"{options.Authority}/connect/userinfo"
                };

                // 3. Now return the configured client
                return new OidcClient(options);
            });

            builder.Services.AddHttpClient("rides", client =>
            {
                client.BaseAddress = new Uri("https://10.0.2.2:5005/");
                client.DefaultRequestHeaders.Accept.Add(
                  new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            builder.Services.AddTransient<MainPage>();
            return builder.Build();
        }
    }
}