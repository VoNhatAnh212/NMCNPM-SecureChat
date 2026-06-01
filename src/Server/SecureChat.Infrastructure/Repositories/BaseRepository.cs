#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Exceptions;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides a generic Entity Framework Core repository implementation.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class BaseRepository<T> : IRepository<T>
    where T : class
{
    /// <summary>The database context used by the repository.</summary>
    protected readonly AppDbContext Context;

    /// <summary>The database set for the entity type.</summary>
    protected readonly DbSet<T> DbSet;

    /// <summary>The logger used by the repository.</summary>
    protected readonly ILogger<BaseRepository<T>> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The repository logger.</param>
    public BaseRepository(AppDbContext context, ILogger<BaseRepository<T>> logger)
    {
        Context = context;
        DbSet = context.Set<T>();
        Logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        Logger.LogDebug("Getting {EntityType} by id {EntityId}.", typeof(T).Name, id);
        return await DbSet.FindAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        Logger.LogDebug("Getting all {EntityType} entities.", typeof(T).Name);
        return await DbSet.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Logger.LogDebug("Adding {EntityType}.", typeof(T).Name);
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public virtual async Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Logger.LogDebug("Updating {EntityType}.", typeof(T).Name);
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(Guid id)
    {
        Logger.LogDebug("Deleting {EntityType} by id {EntityId}.", typeof(T).Name, id);

        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            throw new DomainException($"{typeof(T).Name} '{id}' was not found.", 404);
        }

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        Logger.LogDebug("Checking whether {EntityType} exists by id {EntityId}.", typeof(T).Name, id);
        return await DbSet.AnyAsync(entity => EF.Property<Guid>(entity, "Id") == id);
    }
}
