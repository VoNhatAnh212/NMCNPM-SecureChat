#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="User"/> entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.AvatarUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(user => user.Devices)
            .WithOne(device => device.User)
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.PublicKeys)
            .WithOne(publicKey => publicKey.User)
            .HasForeignKey(publicKey => publicKey.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.SentMessages)
            .WithOne(message => message.Sender)
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
