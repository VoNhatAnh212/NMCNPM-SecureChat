#nullable enable

using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecureChat.Client.Models;
using SecureChat.Client.Services;
using SecureChat.Client.Services.Crypto;
using SecureChat.Shared.DTOs.Auth;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Provides a typed wrapper around <see cref="HttpClient"/>.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KeyStorageService _keyStorageService;
    private readonly AuthSessionService _authSession;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    public ApiClient(KeyStorageService keyStorageService, AuthSessionService authSession)
    {
        _keyStorageService = keyStorageService;
        _authSession = authSession;
        _httpClient = new HttpClient(ServerCertificateValidator.CreateHttpClientHandler())
        {
            BaseAddress = new Uri(ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "https://localhost:5001")
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body)
    {
        using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Post, path) { Content = CreateJsonContent(body) });
        return await ReadResponseAsync<TResponse>(response);
    }

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(string path)
    {
        using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, path));
        return await ReadResponseAsync<TResponse>(response);
    }

    /// <inheritdoc />
    public async Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body)
    {
        using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Put, path) { Content = CreateJsonContent(body) });
        return await ReadResponseAsync<TResponse>(response);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string path)
    {
        using var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Delete, path));
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> PostFileAsync(string path, Stream fileStream, string fileName)
    {
        using var response = await SendWithRetryAsync(() =>
        {
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            return new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        });
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(string path)
    {
        var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, path));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    private StringContent CreateJsonContent<T>(T body)
    {
        return new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json");
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory)
    {
        var response = await SendAsync(requestFactory());
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        var refreshed = await TryRefreshTokenAsync();
        if (!refreshed)
        {
            return await SendAsync(requestFactory());
        }

        return await SendAsync(requestFactory());
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var tokens = _authSession.GetTokens();
        if (string.IsNullOrWhiteSpace(tokens.access))
        {
            tokens = await _keyStorageService.LoadTokensAsync(_authSession.CurrentAccount);
            if (!string.IsNullOrWhiteSpace(tokens.access) && !string.IsNullOrWhiteSpace(tokens.refresh) && tokens.expiry is not null)
            {
                var account = await ResolveAccountForTokenAsync(tokens.access);
                _authSession.SetSession(tokens.access, tokens.refresh, tokens.expiry.Value, account);
            }
        }

        if (!string.IsNullOrWhiteSpace(tokens.access))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.access);
        }

        return await _httpClient.SendAsync(request);
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        var tokens = _authSession.GetTokens();
        if (string.IsNullOrWhiteSpace(tokens.refresh))
        {
            tokens = await _keyStorageService.LoadTokensAsync(_authSession.CurrentAccount);
        }

        if (string.IsNullOrWhiteSpace(tokens.refresh))
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = CreateJsonContent(new RefreshTokenRequestDto { RefreshToken = tokens.refresh })
        };

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var auth = await ReadResponseAsync<LoginVerifyResponseDto>(response);
        await _keyStorageService.SaveTokensAsync(auth.AccessToken, auth.RefreshToken, auth.ExpiresAt.DateTime, _authSession.CurrentAccount);
        _authSession.UpdateTokens(auth.AccessToken, auth.RefreshToken, auth.ExpiresAt.DateTime);
        return true;
    }

    private async Task<LocalUser?> ResolveAccountForTokenAsync(string accessToken)
    {
        var deviceId = AuthSessionService.ExtractStringClaim(accessToken, "deviceId");
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var accounts = await _keyStorageService.LoadAccountsAsync();
        return accounts.FirstOrDefault(account => string.Equals(account.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ExtractErrorMessage(json) ?? $"API request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }

        var wrapped = JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);
        if (wrapped is not null && wrapped.Success)
        {
            return wrapped.Data!;
        }

        if (wrapped is not null && !wrapped.Success)
        {
            throw new InvalidOperationException(wrapped.Error ?? "API request failed.");
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException("API response was empty.");
    }

    private static string? ExtractErrorMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Error);
}
