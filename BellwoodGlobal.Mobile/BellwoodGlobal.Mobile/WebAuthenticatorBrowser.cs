using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;    // for BrowserResult, BrowserOptions
using Microsoft.Maui.Authentication;       // for WebAuthenticatorResult

namespace BellwoodGlobal.Mobile
{
    public class WebAuthenticatorBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
    {
        public async Task<BrowserResult> InvokeAsync(
            BrowserOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Launch the system browser and wait for the callback
                var mauiResult = await WebAuthenticator
                    .Default
                    .AuthenticateAsync(
                        new Uri(options.StartUrl),
                        new Uri(options.EndUrl));

                // Build a query string from the returned parameters
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                foreach (var kv in mauiResult.Properties)
                {
                    query[kv.Key] = kv.Value;
                }

                return new BrowserResult
                {
                    Response = query.ToString(),
                    ResultType = BrowserResultType.Success
                };
            }
            catch (Exception ex)
            {
                return new BrowserResult
                {
                    Error = ex.Message,
                    ResultType = BrowserResultType.UnknownError
                };
            }
        }
    }
}
