#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Friend"/> entity.
/// </summary>
public class FriendConfiguration : IEntityTypeConfiguration<Friend>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Friend> builder)
    {
        builder.ToTable("Friends");

        builder.HasKey(friend => friend.Id);

        builder.Property(friend => friend.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(friend => friend.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(friend => new { friend.UserId, friend.FriendUserId })
            .IsUnique();

        builder.HasOne(friend => friend.User)
            .WithMany()
            .HasForeignKey(friend => friend.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(friend => friend.FriendUser)
            .WithMany()
            .HasForeignKey(friend => friend.FriendUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
