#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines group persistence operations.
/// </summary>
public interface IGroupRepository : IRepository<Group>
{
    /// <summary>Gets groups for a member.</summary>
    /// <param name="memberId">The member user identifier.</param>
    /// <returns>The groups that contain the member.</returns>
    Task<IReadOnlyCollection<Group>> GetByMemberIdAsync(Guid memberId);

    /// <summary>Gets a group with its members loaded.</summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group when found; otherwise, null.</returns>
    Task<Group?> GetWithMembersAsync(Guid groupId);
}
