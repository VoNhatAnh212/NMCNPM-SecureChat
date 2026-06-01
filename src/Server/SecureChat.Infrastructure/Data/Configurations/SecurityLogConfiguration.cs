#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="SecurityLog"/> entity.
/// </summary>
public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        builder.ToTable("SecurityLogs");

        builder.HasKey(securityLog => securityLog.Id);

        builder.Property(securityLog => securityLog.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(securityLog => securityLog.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(securityLog => securityLog.IpAddress)
            .HasMaxLength(100);

        builder.Property(securityLog => securityLog.UserAgent)
            .HasMaxLength(500);

        builder.Property(securityLog => securityLog.Details)
            .HasColumnType("nvarchar(max)");

        builder.Property(securityLog => securityLog.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(securityLog => new { securityLog.UserId, securityLog.CreatedAt });
    }
}
