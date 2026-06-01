#nullable enable

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Defines HTTP API client operations.
/// </summary>
public interface IApiClient
{
    /// <summary>Sends a POST request.</summary>
    Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body);

    /// <summary>Sends a GET request.</summary>
    Task<TResponse> GetAsync<TResponse>(string path);

    /// <summary>Sends a PUT request.</summary>
    Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body);

    /// <summary>Sends a DELETE request.</summary>
    Task<bool> DeleteAsync(string path);

    /// <summary>Posts a file stream.</summary>
    Task<bool> PostFileAsync(string path, Stream fileStream, string fileName);

    /// <summary>Gets a file stream.</summary>
    Task<Stream> GetFileAsync(string path);
}
