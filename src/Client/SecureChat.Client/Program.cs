#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using SecureChat.Client.Forms.Helpers;
using SecureChat.Client.Controllers;
using SecureChat.Client.Forms.Splash;
using SecureChat.Client.Models;
using SecureChat.Client.Services.Api;
using SecureChat.Client.Services.Crypto;
using SecureChat.Client.Services.SignalR;
using SecureChat.Client.Services.Storage;
using SecureChat.Client.Services;

namespace SecureChat.Client;

/// <summary>
/// Provides the Windows Forms application entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplySavedTheme();
        ApplicationConfiguration.Initialize();
        Application.Run(new SplashForm(serviceProvider));
    }

    private static void ApplySavedTheme()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureChat", "AppSettings.json");
            if (!File.Exists(path))
            {
                ThemeHelper.UseDarkTheme();
                return;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
            if (string.Equals(settings?.Theme, "Light", StringComparison.OrdinalIgnoreCase))
            {
                ThemeHelper.UseLightTheme();
            }
            else
            {
                ThemeHelper.UseDarkTheme();
            }
        }
        catch
        {
            ThemeHelper.UseDarkTheme();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ApiClient>();
        services.AddSingleton<IApiClient>(provider => provider.GetRequiredService<ApiClient>());
        services.AddSingleton<RsaService>();
        services.AddSingleton<AesGcmService>();
        services.AddSingleton<KeyStorageService>();
        services.AddSingleton<AuthSessionService>();
        services.AddSingleton<UserDataPathService>();
        services.AddSingleton<ISignalRService, SignalRService>();
        services.AddSingleton<SessionCache>();
        services.AddSingleton<LocalMessageStore>();

        services.AddTransient<AuthApiService>();
        services.AddTransient<ChatApiService>();
        services.AddTransient<FriendApiService>();
        services.AddTransient<UserApiService>();
        services.AddTransient<GroupApiService>();
        services.AddTransient<FileApiService>();
        services.AddTransient<DeviceApiService>();
        services.AddTransient<MessageApiService>();

        services.AddTransient<AuthController>();
        services.AddTransient<ChatController>();
        services.AddTransient<FriendController>();
        services.AddTransient<GroupController>();
    }
}
