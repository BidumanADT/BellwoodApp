using Microsoft.Maui.Storage;
using System.Text.Json;

namespace BellwoodGlobal.Mobile.Services;

public interface IAuthService
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task<bool> IsSignedInAsync();
    Task<string?> GetValidTokenAsync();
    Task RequireSignInAsync(); // navigates to Login if not signed in / expired
    Task LogoutAsync();
}

public sealed class AuthService : IAuthService
{
    private const string TokenKey = "access_token";
    private const int ClockSkewSeconds = 60; // allow a little skew

    public async Task<string?> GetTokenAsync()
    {
        try { return await SecureStorage.GetAsync(TokenKey); }
        catch { return null; }
    }

    public async Task SetTokenAsync(string token)
    {
        await SecureStorage.SetAsync(TokenKey, token);
    }

    public async Task<bool> IsSignedInAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrWhiteSpace(token) && !IsExpired(token);
    }

    public async Task<string?> GetValidTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
        {
            await LogoutAsync(); // clears and routes to Login
            return null;
        }
        return token;
    }

    public async Task RequireSignInAsync()
    {
        if (!await IsSignedInAsync())
            await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    public async Task LogoutAsync()
    {
        try { SecureStorage.Remove(TokenKey); } catch { /* ignore */ }
        // if we're already on LoginPage this will no-op
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    // ------------- helpers -------------

    private static bool IsExpired(string jwt)
    {
        // If we can’t read exp, assume NOT expired and let the server 401 if needed.
        if (!TryReadExp(jwt, out var exp)) return false;
        var now = DateTimeOffset.UtcNow.AddSeconds(ClockSkewSeconds);
        return now >= exp;
    }

    private static bool TryReadExp(string jwt, out DateTimeOffset exp)
    {
        exp = default;

        // JWT: header.payload.signature (we only need payload)
        var parts = jwt.Split('.');
        if (parts.Length < 2) return false;

        try
        {
            var payloadJson = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("exp", out var expProp)) return false;

            long seconds = expProp.ValueKind switch
            {
                JsonValueKind.Number when expProp.TryGetInt64(out var l) => l,
                JsonValueKind.String when long.TryParse(expProp.GetString(), out var ls) => ls,
                _ => 0
            };
            if (seconds <= 0) return false;

            exp = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch { return false; }
    }

    private static string Base64UrlDecode(string input)
    {
        // convert Base64Url -> Base64
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        var bytes = Convert.FromBase64String(s);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
