#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Device"/> entity.
/// </summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(device => device.Id);

        builder.Property(device => device.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(device => device.DeviceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(device => device.Platform)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(device => device.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(device => device.DeviceFingerprint)
            .IsUnique();

        builder.Property(device => device.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(device => device.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(device => device.User)
            .WithMany(user => user.Devices)
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
