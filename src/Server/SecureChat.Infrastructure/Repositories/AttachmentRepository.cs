#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Interfaces.Repositories;
using SecureChat.Infrastructure.Data;

namespace SecureChat.Infrastructure.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence for attachments.
/// </summary>
public class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
{
    private readonly ILogger<AttachmentRepository> _logger;

    /// <summary>Initializes a new instance of the <see cref="AttachmentRepository"/> class.</summary>
    public AttachmentRepository(AppDbContext context, ILogger<AttachmentRepository> logger, ILogger<BaseRepository<Attachment>> baseLogger)
        : base(context, baseLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Attachment>> GetByMessageIdAsync(Guid messageId)
    {
        _logger.LogDebug("Getting attachments for message {MessageId}.", messageId);

        return await Context.Attachments
            .Where(attachment => attachment.MessageId == messageId)
            .ToListAsync();
    }
}
