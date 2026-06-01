#nullable enable

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SecureChat.Client.Services.Crypto;

/// <summary>
/// Provides RSA key generation, signing, encryption, and fingerprint operations.
/// </summary>
public class RsaService
{
    private const int KeySizeBits = 4096;
    private readonly ILogger<RsaService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RsaService"/> class.
    /// </summary>
    public RsaService(ILogger<RsaService>? logger = null)
    {
        _logger = logger ?? NullLogger<RsaService>.Instance;
    }

    /// <summary>Generates a 4096-bit RSA key pair in PEM format.</summary>
    public Task<(string privateKeyPem, string publicKeyPem)> GenerateKeyPairAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create(KeySizeBits);
                return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate RSA key pair.");
                throw new InvalidOperationException("Unable to generate encryption keys.");
            }
        });
    }

    /// <summary>Signs a login challenge with a PEM encoded private key.</summary>
    public Task<string> SignChallengeAsync(string challenge, string privateKeyPem)
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);

                var challengeBytes = Encoding.UTF8.GetBytes(challenge);
                var signature = rsa.SignData(challengeBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign challenge.");
                throw new InvalidOperationException("Unable to sign the login challenge.");
            }
        });
    }

    /// <summary>Encrypts data with a PEM encoded public key.</summary>
    public Task<byte[]> EncryptWithPublicKeyAsync(byte[] data, string publicKeyPem)
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to RSA encrypt data.");
                throw new InvalidOperationException("Unable to encrypt data.");
            }
        });
    }

    /// <summary>Decrypts data with a PEM encoded private key.</summary>
    public Task<byte[]> DecryptWithPrivateKeyAsync(byte[] encryptedData, string privateKeyPem)
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to RSA decrypt data.");
                throw new InvalidOperationException("Unable to decrypt data.");
            }
        });
    }

    /// <summary>Gets a SHA-256 fingerprint for a PEM encoded public key.</summary>
    public Task<string> GetPublicKeyFingerprintAsync(string publicKeyPem)
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);
                var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
                var hash = SHA256.HashData(publicKeyBytes);
                return string.Join(':', hash.Select(value => value.ToString("X2")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate public key fingerprint.");
                throw new InvalidOperationException("Unable to calculate public key fingerprint.");
            }
        });
    }

    /// <summary>Exports the public key PEM from a private key PEM.</summary>
    public Task<string> ExportPublicKeyAsync(string privateKeyPem)
    {
        return Task.Run(() =>
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                return rsa.ExportSubjectPublicKeyInfoPem();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export public key from private key.");
                throw new InvalidOperationException("Unable to export public key.");
            }
        });
    }
}
