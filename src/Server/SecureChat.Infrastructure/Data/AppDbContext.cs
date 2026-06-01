#nullable enable

using Microsoft.EntityFrameworkCore;
using SecureChat.Domain.Entities;
using SecureChat.Infrastructure.Data.Configurations;

namespace SecureChat.Infrastructure.Data;

/// <summary>
/// Represents the Entity Framework Core database context for SecureChat.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets or sets users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets or sets devices.</summary>
    public DbSet<Device> Devices => Set<Device>();

    /// <summary>Gets or sets public keys.</summary>
    public DbSet<PublicKey> PublicKeys => Set<PublicKey>();

    /// <summary>Gets or sets friendships.</summary>
    public DbSet<Friend> Friends => Set<Friend>();

    /// <summary>Gets or sets friend requests.</summary>
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();

    /// <summary>Gets or sets blocked users.</summary>
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();

    /// <summary>Gets or sets groups.</summary>
    public DbSet<Group> Groups => Set<Group>();

    /// <summary>Gets or sets group members.</summary>
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    /// <summary>Gets or sets group keys.</summary>
    public DbSet<GroupKey> GroupKeys => Set<GroupKey>();

    /// <summary>Gets or sets chats.</summary>
    public DbSet<Chat> Chats => Set<Chat>();

    /// <summary>Gets or sets messages.</summary>
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>Gets or sets attachments.</summary>
    public DbSet<Attachment> Attachments => Set<Attachment>();

    /// <summary>Gets or sets refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets or sets OTP requests.</summary>
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();

    /// <summary>Gets or sets security logs.</summary>
    public DbSet<SecurityLog> SecurityLogs => Set<SecurityLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        modelBuilder.ApplyConfiguration(new PublicKeyConfiguration());
        modelBuilder.ApplyConfiguration(new FriendConfiguration());
        modelBuilder.ApplyConfiguration(new FriendRequestConfiguration());
        modelBuilder.ApplyConfiguration(new BlockedUserConfiguration());
        modelBuilder.ApplyConfiguration(new GroupConfiguration());
        modelBuilder.ApplyConfiguration(new GroupMemberConfiguration());
        modelBuilder.ApplyConfiguration(new GroupKeyConfiguration());
        modelBuilder.ApplyConfiguration(new ChatConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new AttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OtpRequestConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityLogConfiguration());
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
