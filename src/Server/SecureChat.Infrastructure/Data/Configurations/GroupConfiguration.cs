#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Group"/> entity.
/// </summary>
public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(group => group.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(group => group.AvatarUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(group => group.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(group => group.Creator)
            .WithMany()
            .HasForeignKey(group => group.CreatorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(group => group.Members)
            .WithOne(member => member.Group)
            .HasForeignKey(member => member.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(group => group.Keys)
            .WithOne(groupKey => groupKey.Group)
            .HasForeignKey(groupKey => groupKey.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
