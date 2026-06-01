#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.File;

/// <summary>
/// Represents upload details returned by the server.
/// </summary>
public record UploadFileResponseDto
{
    /// <summary>Gets the reserved attachment identifier.</summary>
    [JsonPropertyName("attachmentId")]
    public Guid AttachmentId { get; init; }

    /// <summary>Gets the upload URL.</summary>
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; init; } = string.Empty;

    /// <summary>Gets the time when the upload URL expires.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }
}
