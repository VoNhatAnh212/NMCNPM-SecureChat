#nullable enable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Domain.Interfaces.Services;
using SecureChat.Shared.Constants;

namespace SecureChat.Infrastructure.Services;

/// <summary>
/// Provides JWT access token and refresh token generation.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<JwtService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtService"/> class.
    /// </summary>
    public JwtService(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepository, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> GenerateAccessTokenAsync(User user, Device device)
    {
        _logger.LogDebug("Generating access token for user {UserId} and device {DeviceId}.", user.Id, device.Id);

        var signingKey = GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(AppConstants.TOKEN_EXPIRY_MINUTES);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("deviceId", device.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("displayName", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: GetRequiredConfig("Jwt:Issuer"),
            audience: GetRequiredConfig("Jwt:Audience"),
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    /// <inheritdoc />
    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, Guid deviceId)
    {
        _logger.LogDebug("Generating refresh token for user {UserId} and device {DeviceId}.", userId, deviceId);

        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken(userId, deviceId, tokenValue, DateTimeOffset.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(refreshToken);
        return refreshToken;
    }

    /// <inheritdoc />
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        _logger.LogDebug("Validating JWT access token.");

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, GetValidationParameters(), out _);
            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _logger.LogWarning(ex, "JWT validation failed.");
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetClaimsAsync(string token)
    {
        _logger.LogDebug("Getting JWT claims.");

        var principal = await ValidateTokenAsync(token);
        return principal?.Claims
            .GroupBy(claim => claim.Type)
            .ToDictionary(group => group.Key, group => group.First().Value)
            ?? new Dictionary<string, string>();
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var secret = _configuration["Jwt:SecretKey"] ?? GetRequiredConfig("Jwt:Secret");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = GetRequiredConfig("Jwt:Issuer"),
            ValidAudience = GetRequiredConfig("Jwt:Audience"),
            IssuerSigningKey = GetSigningKey(),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    private string GetRequiredConfig(string key)
    {
        return _configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");
    }
}
