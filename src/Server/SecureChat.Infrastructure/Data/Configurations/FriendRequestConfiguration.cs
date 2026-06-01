#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="FriendRequest"/> entity.
/// </summary>
public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("FriendRequests");

        builder.HasKey(friendRequest => friendRequest.Id);

        builder.Property(friendRequest => friendRequest.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(friendRequest => friendRequest.Message)
            .HasMaxLength(500);

        builder.Property(friendRequest => friendRequest.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(friendRequest => friendRequest.SentAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(friendRequest => friendRequest.FromUser)
            .WithMany()
            .HasForeignKey(friendRequest => friendRequest.FromUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(friendRequest => friendRequest.ToUser)
            .WithMany()
            .HasForeignKey(friendRequest => friendRequest.ToUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
