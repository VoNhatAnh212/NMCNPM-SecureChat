#nullable enable

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Interfaces.Services;
using SecureChat.Shared.Constants;

namespace SecureChat.Infrastructure.Services;

/// <summary>
/// Provides short-lived login challenge generation and RSA signature validation.
/// </summary>
public class ChallengeService : IChallengeService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ChallengeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChallengeService"/> class.
    /// </summary>
    public ChallengeService(IMemoryCache memoryCache, ILogger<ChallengeService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> GenerateChallengeAsync(Guid deviceId)
    {
        _logger.LogDebug("Generating login challenge for device {DeviceId}.", deviceId);

        var challenge = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _memoryCache.Set(GetCacheKey(deviceId), challenge, TimeSpan.FromSeconds(AppConstants.CHALLENGE_EXPIRY_SECONDS));
        return Task.FromResult(challenge);
    }

    /// <inheritdoc />
    public Task<bool> ValidateChallengeAsync(Guid deviceId, string signedChallenge, string publicKeyPem)
    {
        _logger.LogDebug("Validating login challenge for device {DeviceId}.", deviceId);

        var cacheKey = GetCacheKey(deviceId);
        if (!_memoryCache.TryGetValue(cacheKey, out string? challenge) || string.IsNullOrWhiteSpace(challenge))
        {
            return Task.FromResult(false);
        }

        _memoryCache.Remove(cacheKey);

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var challengeBytes = Encoding.UTF8.GetBytes(challenge);
            var signatureBytes = Convert.FromBase64String(signedChallenge);

            var isValid = rsa.VerifyData(challengeBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(isValid);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException or FormatException)
        {
            _logger.LogWarning(ex, "Challenge validation failed for device {DeviceId}.", deviceId);
            return Task.FromResult(false);
        }
    }

    private static string GetCacheKey(Guid deviceId)
    {
        return $"challenge:{deviceId}";
    }
}
