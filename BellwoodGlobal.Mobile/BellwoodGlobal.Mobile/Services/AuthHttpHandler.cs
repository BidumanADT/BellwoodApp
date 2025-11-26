using System.Net.Http;
using System.Net.Http.Headers;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class AuthHttpHandler : DelegatingHandler
    {
        private readonly IAuthService _auth;

        public AuthHttpHandler(IAuthService auth)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Health endpoints stay anonymous; everything else carries the bearer token.
            if (!IsHealthCheck(request.RequestUri) && request.Headers.Authorization is null)
            {
                var token = await _auth.GetValidTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token likely expired/invalid; force re-auth so the next call can retry with a fresh JWT.
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await _auth.LogoutAsync();
                });
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Surface a friendlier error upstream.
                response.ReasonPhrase ??= "Not authorized";
            }

            return response;
        }

        private static bool IsHealthCheck(Uri? uri)
        {
            if (uri is null) return false;
            var path = uri.AbsolutePath.ToLowerInvariant();
            return path.EndsWith("/health") || path.EndsWith("/healthz");
        }
    }
}
