#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="GroupMember"/> entity.
/// </summary>
public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("GroupMembers");

        builder.HasKey(groupMember => groupMember.Id);

        builder.Property(groupMember => groupMember.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(groupMember => groupMember.Role)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(groupMember => groupMember.JoinedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(groupMember => new { groupMember.GroupId, groupMember.UserId })
            .IsUnique();

        builder.HasOne(groupMember => groupMember.Group)
            .WithMany(group => group.Members)
            .HasForeignKey(groupMember => groupMember.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(groupMember => groupMember.User)
            .WithMany()
            .HasForeignKey(groupMember => groupMember.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
