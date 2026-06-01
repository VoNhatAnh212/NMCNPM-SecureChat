#nullable enable

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines common asynchronous repository operations for an aggregate.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T>
    where T : class
{
    /// <summary>Gets an entity by its identifier.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity when found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>Gets all entities.</summary>
    /// <returns>The entities.</returns>
    Task<IReadOnlyCollection<T>> GetAllAsync();

    /// <summary>Adds an entity.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(T entity);

    /// <summary>Updates an entity.</summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(T entity);

    /// <summary>Deletes an entity by its identifier.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>Determines whether an entity exists.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>True when the entity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id);
}
