#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Group;

/// <summary>
/// Represents an encrypted group key for a recipient device.
/// </summary>
public record GroupKeyDto
{
    /// <summary>Gets the group identifier.</summary>
    [JsonPropertyName("groupId")]
    public Guid GroupId { get; init; }

    /// <summary>Gets the encrypted group key.</summary>
    [JsonPropertyName("encryptedGroupKey")]
    public string EncryptedGroupKey { get; init; } = string.Empty;

    /// <summary>Gets the recipient device identifier.</summary>
    [JsonPropertyName("forDeviceId")]
    public Guid ForDeviceId { get; init; }

    /// <summary>Gets the group key version.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; }
}
