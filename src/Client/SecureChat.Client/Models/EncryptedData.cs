#nullable enable

namespace SecureChat.Client.Models;

/// <summary>
/// Represents authenticated encrypted data.
/// </summary>
/// <param name="IV">The initialization vector.</param>
/// <param name="Ciphertext">The encrypted bytes.</param>
/// <param name="AuthTag">The authentication tag.</param>
public record EncryptedData(byte[] IV, byte[] Ciphertext, byte[] AuthTag);
