#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="RefreshToken"/> entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(refreshToken => refreshToken.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();

        builder.HasIndex(refreshToken => new { refreshToken.UserId, refreshToken.DeviceId, refreshToken.IsRevoked });

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(refreshToken => refreshToken.Device)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
