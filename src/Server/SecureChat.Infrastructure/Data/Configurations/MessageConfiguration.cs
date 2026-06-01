#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Message"/> entity.
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(message => message.EncryptedContent)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(message => message.IV)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(message => message.AuthTag)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(message => message.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(message => message.MessageType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(message => message.SentAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(message => new { message.ChatId, message.SentAt });

        builder.HasOne(message => message.Chat)
            .WithMany(chat => chat.Messages)
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(message => message.Sender)
            .WithMany(user => user.SentMessages)
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(message => message.Attachments)
            .WithOne(attachment => attachment.Message)
            .HasForeignKey(attachment => attachment.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
