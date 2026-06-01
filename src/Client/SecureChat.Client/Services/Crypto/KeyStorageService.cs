#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SecureChat.Client.Models;
using SecureChat.Client.Services.Storage;

namespace SecureChat.Client.Services.Crypto;

/// <summary>
/// Stores private keys and tokens using Windows DPAPI.
/// </summary>
public class KeyStorageService
{
    private readonly string _basePath;
    private readonly string _keysPath;
    private readonly string _tokensPath;
    private readonly string _accountsPath;
    private readonly ILogger<KeyStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyStorageService"/> class.
    /// </summary>
    public KeyStorageService(ILogger<KeyStorageService>? logger = null)
    {
        _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat");
        _keysPath = Path.Combine(_basePath, "keys");
        _tokensPath = Path.Combine(_basePath, "tokens.dat");
        _accountsPath = Path.Combine(_basePath, "accounts.json");
        _logger = logger ?? NullLogger<KeyStorageService>.Instance;
    }

    /// <summary>Saves a PEM encoded private key protected by DPAPI.</summary>
    public async Task SavePrivateKeyAsync(string deviceId, string privateKeyPem)
    {
        try
        {
            Directory.CreateDirectory(_keysPath);
            var privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyPem);
            var protectedBytes = ProtectedData.Protect(privateKeyBytes, null, DataProtectionScope.CurrentUser);
            await System.IO.File.WriteAllBytesAsync(GetKeyPath(deviceId), protectedBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            _logger.LogError(ex, "Failed to save private key.");
            throw new InvalidOperationException("Unable to save private key.");
        }
    }

    /// <summary>Loads a PEM encoded private key protected by DPAPI.</summary>
    public async Task<string?> LoadPrivateKeyAsync(string deviceId)
    {
        try
        {
            var path = GetKeyPath(deviceId);
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            var protectedBytes = await System.IO.File.ReadAllBytesAsync(path);
            var privateKeyBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(privateKeyBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            _logger.LogError(ex, "Failed to load private key.");
            return null;
        }
    }

    /// <summary>Deletes the stored private key for a device.</summary>
    public Task DeletePrivateKeyAsync(string deviceId)
    {
        try
        {
            var path = GetKeyPath(deviceId);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to delete private key.");
            throw new InvalidOperationException("Unable to delete private key.");
        }
    }

    /// <summary>Checks whether a private key exists for a device.</summary>
    public Task<bool> HasPrivateKeyAsync(string deviceId)
    {
        return Task.FromResult(System.IO.File.Exists(GetKeyPath(deviceId)));
    }

    /// <summary>Saves access and refresh tokens protected by DPAPI.</summary>
    public async Task SaveTokensAsync(string accessToken, string refreshToken, DateTime expiresAt, LocalUser? account = null)
    {
        try
        {
            var path = GetTokenPath(account);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var payload = JsonSerializer.Serialize(new TokenStore(accessToken, refreshToken, expiresAt));
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(payload), null, DataProtectionScope.CurrentUser);
            await System.IO.File.WriteAllBytesAsync(path, protectedBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            _logger.LogError(ex, "Failed to save tokens.");
            throw new InvalidOperationException("Unable to save authentication tokens.");
        }
    }

    /// <summary>Loads access and refresh tokens protected by DPAPI.</summary>
    public async Task<(string? access, string? refresh, DateTime? expiry)> LoadTokensAsync(LocalUser? account = null)
    {
        try
        {
            var path = GetTokenPath(account);
            if (!System.IO.File.Exists(path) && account is not null)
            {
                path = _tokensPath;
            }

            if (!System.IO.File.Exists(path))
            {
                return (null, null, null);
            }

            var protectedBytes = await System.IO.File.ReadAllBytesAsync(path);
            var jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var tokenStore = JsonSerializer.Deserialize<TokenStore>(Encoding.UTF8.GetString(jsonBytes));
            return (tokenStore?.AccessToken, tokenStore?.RefreshToken, tokenStore?.ExpiresAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            _logger.LogError(ex, "Failed to load tokens.");
            return (null, null, null);
        }
    }

    /// <summary>Clears stored tokens.</summary>
    public Task ClearTokensAsync(LocalUser? account = null)
    {
        try
        {
            var path = GetTokenPath(account);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to clear tokens.");
            throw new InvalidOperationException("Unable to clear authentication tokens.");
        }
    }

    /// <summary>Saves a local account to the account list.</summary>
    public async Task SaveAccountAsync(LocalUser user)
    {
        var accounts = await LoadAccountsAsync();
        var existing = accounts.FindIndex(account => account.Id == user.Id && account.DeviceId == user.DeviceId);
        if (existing >= 0)
        {
            accounts[existing] = user;
        }
        else
        {
            accounts.Add(user);
        }

        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(UserDataPathService.GetRoamingPath(user, string.Empty));
        Directory.CreateDirectory(UserDataPathService.GetLocalPath(user, string.Empty));
        var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(_accountsPath, json);
    }

    /// <summary>Loads local accounts.</summary>
    public async Task<List<LocalUser>> LoadAccountsAsync()
    {
        try
        {
            if (!System.IO.File.Exists(_accountsPath))
            {
                return new List<LocalUser>();
            }

            var json = await System.IO.File.ReadAllTextAsync(_accountsPath);
            return JsonSerializer.Deserialize<List<LocalUser>>(json) ?? new List<LocalUser>();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex, "Failed to load local accounts.");
            return new List<LocalUser>();
        }
    }

    private string GetKeyPath(string deviceId)
    {
        return Path.Combine(_keysPath, $"{deviceId}.key");
    }

    private string GetTokenPath(LocalUser? account)
    {
        return account is null ? _tokensPath : UserDataPathService.GetRoamingPath(account, "tokens.dat");
    }

    private sealed record TokenStore(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
