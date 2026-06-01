#nullable enable

using System.Text.Json;

namespace SecureChat.Client.Services.Storage;

/// <summary>
/// Stores decrypted plaintext messages locally for the current Windows user.
/// </summary>
public class LocalMessageStore
{
    private readonly string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat", "messages");

    /// <summary>Saves decrypted plaintext for a message.</summary>
    public async Task SavePlaintextAsync(Guid messageId, string plaintext)
    {
        Directory.CreateDirectory(_basePath);
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
        return Path.Combine(_basePath, $"{messageId:N}.json");
    }

    private sealed record StoredMessage(string Plaintext, DateTimeOffset SavedAt);
}
