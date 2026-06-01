#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="GroupKey"/> entity.
/// </summary>
public class GroupKeyConfiguration : IEntityTypeConfiguration<GroupKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GroupKey> builder)
    {
        builder.ToTable("GroupKeys");

        builder.HasKey(groupKey => groupKey.Id);

        builder.Property(groupKey => groupKey.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(groupKey => groupKey.EncryptedGroupKey)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(groupKey => groupKey.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(groupKey => groupKey.Group)
            .WithMany(group => group.Keys)
            .HasForeignKey(groupKey => groupKey.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(groupKey => groupKey.Device)
            .WithMany()
            .HasForeignKey(groupKey => groupKey.ForDeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
