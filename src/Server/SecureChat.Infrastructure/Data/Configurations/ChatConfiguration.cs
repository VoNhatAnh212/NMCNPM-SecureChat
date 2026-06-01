#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Chat"/> entity.
/// </summary>
public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ToTable("Chats");

        builder.HasKey(chat => chat.Id);

        builder.Property(chat => chat.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(chat => chat.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(chat => chat.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(chat => chat.Group)
            .WithMany()
            .HasForeignKey(chat => chat.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(chat => chat.Messages)
            .WithOne(message => message.Chat)
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(chat => chat.Participants)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "ChatParticipants",
                right => right.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Chat>().WithMany().HasForeignKey("ChatId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("ChatId", "UserId");
                    join.ToTable("ChatParticipants");
                });
    }
}
