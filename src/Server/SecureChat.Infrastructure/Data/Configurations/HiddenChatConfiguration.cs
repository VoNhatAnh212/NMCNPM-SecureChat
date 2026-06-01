#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the <see cref="HiddenChat"/> entity.
/// </summary>
public class HiddenChatConfiguration : IEntityTypeConfiguration<HiddenChat>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HiddenChat> builder)
    {
        builder.ToTable("HiddenChats");

        builder.HasKey(hiddenChat => hiddenChat.Id);

        builder.Property(hiddenChat => hiddenChat.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(hiddenChat => hiddenChat.HiddenAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(hiddenChat => new { hiddenChat.ChatId, hiddenChat.UserId })
            .IsUnique();

        builder.HasOne(hiddenChat => hiddenChat.Chat)
            .WithMany()
            .HasForeignKey(hiddenChat => hiddenChat.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(hiddenChat => hiddenChat.User)
            .WithMany()
            .HasForeignKey(hiddenChat => hiddenChat.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
