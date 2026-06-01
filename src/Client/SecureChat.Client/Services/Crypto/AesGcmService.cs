#nullable enable

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SecureChat.Client.Models;
using SecureChat.Client.Services.Storage;

namespace SecureChat.Client.Services.Crypto;

/// <summary>
/// Provides AES-GCM encryption and decryption operations.
/// </summary>
public class AesGcmService
{
    private const int KeySizeBytes = 32;
    private const int IvSizeBytes = 12;
    private const int AuthTagSizeBytes = 16;
    private readonly UserDataPathService? _userDataPathService;
    private readonly ILogger<AesGcmService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesGcmService"/> class.
    /// </summary>
    public AesGcmService(UserDataPathService? userDataPathService = null, ILogger<AesGcmService>? logger = null)
    {
        _userDataPathService = userDataPathService;
        _logger = logger ?? NullLogger<AesGcmService>.Instance;
    }

    /// <summary>Generates a 256-bit AES key.</summary>
    public Task<byte[]> GenerateKeyAsync()
    {
        return Task.Run(() => RandomNumberGenerator.GetBytes(KeySizeBytes));
    }

    /// <summary>Generates a 96-bit AES-GCM IV.</summary>
    public Task<byte[]> GenerateIVAsync()
    {
        return Task.Run(() => RandomNumberGenerator.GetBytes(IvSizeBytes));
    }

    /// <summary>Encrypts plaintext bytes with AES-GCM.</summary>
    public Task<(byte[] ciphertext, byte[] authTag)> EncryptAsync(byte[] plaintext, byte[] key, byte[] iv)
    {
        return Task.Run(() =>
        {
            try
            {
                var ciphertext = new byte[plaintext.Length];
                var authTag = new byte[AuthTagSizeBytes];
                using var aesGcm = new AesGcm(key, AuthTagSizeBytes);
                aesGcm.Encrypt(iv, plaintext, ciphertext, authTag);
                return (ciphertext, authTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AES-GCM encryption failed.");
                throw new InvalidOperationException("Unable to encrypt data.");
            }
        });
    }

    /// <summary>Encrypts a UTF-8 string with AES-GCM.</summary>
    public async Task<EncryptedData> EncryptStringAsync(string plaintext, byte[] key)
    {
        var iv = await GenerateIVAsync();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = await EncryptAsync(plaintextBytes, key, iv);
        return new EncryptedData(iv, encrypted.ciphertext, encrypted.authTag);
    }

    /// <summary>Decrypts AES-GCM ciphertext bytes.</summary>
    public Task<byte[]> DecryptAsync(byte[] ciphertext, byte[] key, byte[] iv, byte[] authTag)
    {
        return Task.Run(() =>
        {
            try
            {
                var plaintext = new byte[ciphertext.Length];
                using var aesGcm = new AesGcm(key, AuthTagSizeBytes);
                aesGcm.Decrypt(iv, ciphertext, authTag, plaintext);
                return plaintext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AES-GCM decryption failed.");
                throw new InvalidOperationException("Unable to decrypt data.");
            }
        });
    }

    /// <summary>Decrypts AES-GCM encrypted data to a UTF-8 string.</summary>
    public async Task<string> DecryptStringAsync(EncryptedData data, byte[] key)
    {
        var plaintext = await DecryptAsync(data.Ciphertext, key, data.IV, data.AuthTag);
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>Encrypts a file into a temporary encrypted file.</summary>
    public async Task<(string outputPath, byte[] iv, byte[] authTag)> EncryptFileAsync(string inputPath, byte[] key)
    {
        try
        {
            var iv = await GenerateIVAsync();
            var plaintext = await System.IO.File.ReadAllBytesAsync(inputPath);
            var encrypted = await EncryptAsync(plaintext, key, iv);
            var outputPath = Path.Combine(GetTempPath(), $"{Guid.NewGuid():N}.securechat");
            await System.IO.File.WriteAllBytesAsync(outputPath, encrypted.ciphertext);
            return (outputPath, iv, encrypted.authTag);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "File encryption failed.");
            throw new InvalidOperationException("Unable to encrypt file.");
        }
    }

    /// <summary>Decrypts a temporary encrypted file into a plaintext temporary file.</summary>
    public async Task<string> DecryptFileAsync(string encryptedPath, byte[] key, byte[] iv, byte[] authTag)
    {
        try
        {
            var ciphertext = await System.IO.File.ReadAllBytesAsync(encryptedPath);
            var plaintext = await DecryptAsync(ciphertext, key, iv, authTag);
            var outputPath = Path.Combine(GetTempPath(), $"{Guid.NewGuid():N}.securechat.dec");
            await System.IO.File.WriteAllBytesAsync(outputPath, plaintext);
            return outputPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "File decryption failed.");
            throw new InvalidOperationException("Unable to decrypt file.");
        }
    }

    private string GetTempPath()
    {
        var path = _userDataPathService?.GetLocalPath("Temp") ?? Path.GetTempPath();
        Directory.CreateDirectory(path);
        return path;
    }
}
