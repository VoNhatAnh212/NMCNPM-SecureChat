#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for OTP requests.
/// </summary>
public class OtpRepository : BaseRepository<OtpRequest>, IOtpRepository
{
    private readonly ILogger<OtpRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="OtpRepository"/> class.</summary>
    public OtpRepository(AppDbContext context, ILogger<OtpRepository> logger, ILogger<BaseRepository<OtpRequest>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OtpRequest?> GetValidOtpAsync(string email, string otpCode)
    {
        _logger.LogDebug("Getting valid OTP for email {Email}.", email);

        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var utcNow = DateTimeOffset.UtcNow;

        return await Context.OtpRequests
            .Where(otpRequest => otpRequest.Email.Trim().ToLower() == normalizedEmail)
            .Where(otpRequest => otpRequest.OtpCode == otpCode)
            .Where(otpRequest => !otpRequest.IsUsed)
            .Where(otpRequest => otpRequest.ExpiresAt > utcNow)
            .OrderByDescending(otpRequest => otpRequest.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task IncrementAttemptsAsync(Guid id)
    {
        _logger.LogDebug("Incrementing OTP attempts for {OtpRequestId}.", id);

        var affectedRows = await Context.OtpRequests
            .Where(otpRequest => otpRequest.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(otpRequest => otpRequest.Attempts, otpRequest => otpRequest.Attempts + 1));

        if (affectedRows == 0)
        {
            throw new DomainException($"OTP request '{id}' was not found.", 404);
        }
    }
}
