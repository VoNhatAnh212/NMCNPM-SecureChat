#nullable enable

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Reserved device API service for future device-specific workflows.
/// </summary>
public class DeviceApiService
{
    /// <summary>Initializes a new instance of the <see cref="DeviceApiService"/> class.</summary>
    public DeviceApiService(AuthApiService authApiService)
    {
        AuthApiService = authApiService;
    }

    /// <summary>Gets the auth API service that currently owns device endpoints.</summary>
    public AuthApiService AuthApiService { get; }
}
