#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="PublicKey"/> entity.
/// </summary>
public class PublicKeyConfiguration : IEntityTypeConfiguration<PublicKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicKey> builder)
    {
        builder.ToTable("PublicKeys");

        builder.HasKey(publicKey => publicKey.Id);

        builder.Property(publicKey => publicKey.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(publicKey => publicKey.PublicKeyPem)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(publicKey => publicKey.KeyFingerprint)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(publicKey => publicKey.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(publicKey => new { publicKey.UserId, publicKey.DeviceId, publicKey.IsActive });

        builder.HasOne(publicKey => publicKey.User)
            .WithMany(user => user.PublicKeys)
            .HasForeignKey(publicKey => publicKey.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(publicKey => publicKey.Device)
            .WithOne(device => device.PublicKey)
            .HasForeignKey<PublicKey>(publicKey => publicKey.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
