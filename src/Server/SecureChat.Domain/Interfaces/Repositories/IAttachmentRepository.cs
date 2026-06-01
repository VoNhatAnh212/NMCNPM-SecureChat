#nullable enable

using SecureChat.Domain.Entities;

namespace SecureChat.Domain.Interfaces.Repositories;

/// <summary>
/// Defines attachment persistence operations.
/// </summary>
public interface IAttachmentRepository : IRepository<Attachment>
{
    /// <summary>Gets attachments for a message.</summary>
    /// <param name="messageId">The message identifier.</param>
    /// <returns>The attachments for the message.</returns>
    Task<IReadOnlyCollection<Attachment>> GetByMessageIdAsync(Guid messageId);
}
