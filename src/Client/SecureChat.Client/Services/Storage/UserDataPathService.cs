#nullable enable

using SecureChat.Client.Models;
using SecureChat.Client.Services;

namespace SecureChat.Client.Services.Storage;

/// <summary>
/// Resolves per-user local storage folders for the current SecureChat process session.
/// </summary>
public class UserDataPathService
{
    private readonly AuthSessionService _authSession;

    /// <summary>Initializes a new instance of the <see cref="UserDataPathService"/> class.</summary>
    public UserDataPathService(AuthSessionService authSession)
    {
        _authSession = authSession;
    }

    /// <summary>Gets a roaming AppData path for the current SecureChat user.</summary>
    public string GetRoamingPath(string childPath)
    {
        var account = _authSession.CurrentAccount;
        return account is null
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat", "users", "_unknown", childPath)
            : GetRoamingPath(account, childPath);
    }

    /// <summary>Gets a local AppData path for the current SecureChat user.</summary>
    public string GetLocalPath(string childPath)
    {
        var account = _authSession.CurrentAccount;
        return account is null
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecureChat", "users", "_unknown", childPath)
            : GetLocalPath(account, childPath);
    }

    /// <summary>Gets a roaming AppData path for a specific SecureChat user.</summary>
    public static string GetRoamingPath(LocalUser account, string childPath)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat", "users", GetUserFolderName(account), childPath);
    }

    /// <summary>Gets a local AppData path for a specific SecureChat user.</summary>
    public static string GetLocalPath(LocalUser account, string childPath)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecureChat", "users", GetUserFolderName(account), childPath);
    }

    /// <summary>Gets the folder name used for a specific SecureChat user.</summary>
    public static string GetUserFolderName(LocalUser account)
    {
        var source = string.IsNullOrWhiteSpace(account.Email) ? account.Id.ToString("N") : account.Email.Trim().ToLowerInvariant();
        var safe = new string(source.Select(character =>
                char.IsLetterOrDigit(character) || character is '.' or '-' or '_' or '@'
                    ? character
                    : '_')
            .ToArray());
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = account.Id.ToString("N");
        }

        return $"{safe}_{account.Id.ToString("N")[..8]}";
    }
}
