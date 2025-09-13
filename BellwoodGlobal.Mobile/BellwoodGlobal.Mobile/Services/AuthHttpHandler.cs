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
            // only add if not set already
            if (request.Headers.Authorization is null)
            {
                var token = await _auth.GetValidTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            // go to login if 401
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await _auth.LogoutAsync();
                });
            }

            return response;
        }
    }
}
