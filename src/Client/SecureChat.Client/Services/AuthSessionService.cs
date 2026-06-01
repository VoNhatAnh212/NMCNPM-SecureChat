#nullable enable

using System.Text;
using System.Text.Json;
using SecureChat.Client.Models;

namespace SecureChat.Client.Services;

/// <summary>
/// Keeps the authenticated user for the current running client process.
/// </summary>
public class AuthSessionService
{
    private readonly object _sync = new();

    private string? _accessToken;
    private string? _refreshToken;
    private DateTime? _expiresAt;
    private LocalUser? _currentAccount;

    /// <summary>Gets the current local account for this running process.</summary>
    public LocalUser? CurrentAccount
    {
        get
        {
            lock (_sync)
            {
                return _currentAccount;
            }
        }
    }

    /// <summary>Gets the current access token for this running process.</summary>
    public string? AccessToken
    {
        get
        {
            lock (_sync)
            {
                return _accessToken;
            }
        }
    }

    /// <summary>Sets the authenticated process session.</summary>
    public void SetSession(string accessToken, string refreshToken, DateTime expiresAt, LocalUser? account)
    {
        lock (_sync)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _expiresAt = expiresAt;
            _currentAccount = account;
        }
    }

    /// <summary>Updates only tokens after a refresh.</summary>
    public void UpdateTokens(string accessToken, string refreshToken, DateTime expiresAt)
    {
        lock (_sync)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _expiresAt = expiresAt;
        }
    }

    /// <summary>Gets the current token snapshot.</summary>
    public (string? access, string? refresh, DateTime? expiry) GetTokens()
    {
        lock (_sync)
        {
            return (_accessToken, _refreshToken, _expiresAt);
        }
    }

    /// <summary>Clears this process session.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _accessToken = null;
            _refreshToken = null;
            _expiresAt = null;
            _currentAccount = null;
        }
    }

    /// <summary>Extracts a string claim from a JWT.</summary>
    public static string? ExtractStringClaim(string? accessToken, string claimName)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
