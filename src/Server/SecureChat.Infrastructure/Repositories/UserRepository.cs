#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for users.
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The repository logger.</param>
    /// <param name="baseLogger">The base repository logger.</param>
    public UserRepository(AppDbContext context, ILogger<UserRepository> logger, ILogger<BaseRepository<User>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        _logger.LogDebug("Getting user by email {Email}.", email);
        var normalizedEmail = email.ToLowerInvariant();

        return await Context.Users
            .Include(user => user.Devices)
            .Include(user => user.PublicKeys)
            .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<User>> GetOnlineUsersAsync()
    {
        _logger.LogDebug("Getting online users.");

        return await Context.Users
            .Where(user => user.IsOnline)
            .Include(user => user.Devices)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<User>> SearchAsync(string query, int limit)
    {
        _logger.LogDebug("Searching users by query {Query} with limit {Limit}.", query, limit);

        var normalizedQuery = query.ToLowerInvariant();
        var take = Math.Max(1, limit);

        return await Context.Users
            .Where(user => user.DisplayName.ToLower().Contains(normalizedQuery) || user.Email.ToLower().Contains(normalizedQuery))
            .OrderBy(user => user.DisplayName)
            .Take(take)
            .ToListAsync();
    }
}
