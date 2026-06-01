#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.File;

/// <summary>
/// Represents an encrypted file attachment.
/// </summary>
public record AttachmentDto
{
    /// <summary>Gets the attachment identifier.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>Gets the original file name.</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    /// <summary>Gets the file size in bytes.</summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; init; }

    /// <summary>Gets the MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; init; } = string.Empty;

    /// <summary>Gets the encrypted storage URL.</summary>
    [JsonPropertyName("encryptedUrl")]
    public string EncryptedUrl { get; init; } = string.Empty;

    /// <summary>Gets the encrypted file key.</summary>
    [JsonPropertyName("encryptedFileKey")]
    public string EncryptedFileKey { get; init; } = string.Empty;

    /// <summary>Gets the initialization vector.</summary>
    [JsonPropertyName("iv")]
    public string IV { get; init; } = string.Empty;

    /// <summary>Gets the authentication tag.</summary>
    [JsonPropertyName("authTag")]
    public string AuthTag { get; init; } = string.Empty;
}
