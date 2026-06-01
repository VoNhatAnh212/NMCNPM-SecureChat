#nullable enable

using System;
using System.Text.Json.Serialization;

namespace SecureChat.Shared.DTOs.Auth;

/// <summary>
/// Represents the response returned after user registration.
/// </summary>
public record RegisterResponseDto
{
    /// <summary>Gets the created user's identifier.</summary>
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    /// <summary>Gets the registration result message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
