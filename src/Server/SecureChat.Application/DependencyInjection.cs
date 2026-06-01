#nullable enable

using Microsoft.Extensions.DependencyInjection;
using SecureChat.Application.Services.Auth;
using SecureChat.Application.Services.Device;
using SecureChat.Application.Services.Friend;
using SecureChat.Application.Services.Group;
using SecureChat.Application.Services.Message;
using SecureChat.Application.Services.File;

namespace SecureChat.Application;

/// <summary>
/// Provides dependency injection registration for the application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IFileService, FileService>();
        return services;
    }
}
