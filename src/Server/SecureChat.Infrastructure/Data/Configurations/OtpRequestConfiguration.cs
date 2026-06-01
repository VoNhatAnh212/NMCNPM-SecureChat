#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="OtpRequest"/> entity.
/// </summary>
public class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OtpRequest> builder)
    {
        builder.ToTable("OtpRequests");

        builder.HasKey(otpRequest => otpRequest.Id);

        builder.Property(otpRequest => otpRequest.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(otpRequest => otpRequest.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(otpRequest => otpRequest.Email);

        builder.Property(otpRequest => otpRequest.OtpCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(otpRequest => otpRequest.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
