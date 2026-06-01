#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="BlockedUser"/> entity.
/// </summary>
public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.ToTable("BlockedUsers");

        builder.HasKey(blockedUser => blockedUser.Id);

        builder.Property(blockedUser => blockedUser.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(blockedUser => blockedUser.BlockedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(blockedUser => new { blockedUser.UserId, blockedUser.BlockedUserId })
            .IsUnique();

        builder.HasOne(blockedUser => blockedUser.User)
            .WithMany()
            .HasForeignKey(blockedUser => blockedUser.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(blockedUser => blockedUser.BlockedUserNav)
            .WithMany()
            .HasForeignKey(blockedUser => blockedUser.BlockedUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
