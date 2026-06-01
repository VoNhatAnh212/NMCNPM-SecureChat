#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="Attachment"/> entity.
/// </summary>
public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(attachment => attachment.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment => attachment.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attachment => attachment.EncryptedStoragePath)
            .IsRequired()
            .HasColumnType("nvarchar(1000)");

        builder.Property(attachment => attachment.EncryptedFileKey)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(attachment => attachment.IV)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(attachment => attachment.AuthTag)
            .IsRequired()
            .HasColumnType("varbinary(max)");

        builder.Property(attachment => attachment.UploadedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(attachment => attachment.Message)
            .WithMany(message => message.Attachments)
            .HasForeignKey(attachment => attachment.MessageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
