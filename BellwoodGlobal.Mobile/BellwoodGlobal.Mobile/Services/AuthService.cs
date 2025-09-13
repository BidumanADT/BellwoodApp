using Microsoft.Maui.Storage;

namespace BellwoodGlobal.Mobile.Services
{
    public interface IAuthService
    {
        Task<string?> GetTokenAsync();
        Task SetTokenAsync(string token);
        Task<bool> IsSignedInAsync();
        Task RequireSignInAsync(); // enforce sign-in if not signed in
        Task LogoutAsync(); // clear token and go to login page
    }

    public sealed class AuthService : IAuthService
    { 
        private const string TokenKey = "access_token";

        public async Task<string?> GetTokenAsync()
        {
            try { return await SecureStorage.GetAsync(TokenKey); }
            catch { return null; } // SecureStorage may not be supported on all platforms
        }

        public async Task SetTokenAsync(string token)
        {
            // persist token securely
            await SecureStorage.SetAsync(TokenKey, token); 
        }

        public async Task<bool> IsSignedInAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrWhiteSpace(token);
        }

        public async Task RequireSignInAsync()
        {
            if (!await IsSignedInAsync())
            {
                // navigate to login page
                await Shell.Current.GoToAsync(nameof(LoginPage));
            }
        }

        public async Task LogoutAsync()
        {
            try { SecureStorage.Remove(TokenKey); }
            catch { /* ignore */ }

            // navigate to login page
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }

}
