#nullable enable

using System.Text.Json;

namespace SecureChat.Client.Services.Storage;

/// <summary>
/// Stores decrypted plaintext messages locally for the current Windows user.
/// </summary>
public class LocalMessageStore
{
    private readonly UserDataPathService _userDataPathService;

    /// <summary>Initializes a new instance of the <see cref="LocalMessageStore"/> class.</summary>
    public LocalMessageStore(UserDataPathService userDataPathService)
    {
        _userDataPathService = userDataPathService;
    }

    /// <summary>Saves decrypted plaintext for a message.</summary>
    public async Task SavePlaintextAsync(Guid messageId, string plaintext)
    {
        var basePath = GetBasePath();
        Directory.CreateDirectory(basePath);
        var path = GetPath(messageId);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new StoredMessage(plaintext, DateTimeOffset.UtcNow)));
    }

    /// <summary>Loads decrypted plaintext for a message.</summary>
    public async Task<string?> LoadPlaintextAsync(Guid messageId)
    {
        var path = GetPath(messageId);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredMessage>(await File.ReadAllTextAsync(path));
            return stored?.Plaintext;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string GetPath(Guid messageId)
    {
        return Path.Combine(GetBasePath(), $"{messageId:N}.json");
    }

    private string GetBasePath()
    {
        return _userDataPathService.GetRoamingPath("messages");
    }

    private sealed record StoredMessage(string Plaintext, DateTimeOffset SavedAt);
}
