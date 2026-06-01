#nullable enable

namespace SecureChat.Server.Responses;

/// <summary>
/// Represents a consistent API response envelope.
/// </summary>
/// <typeparam name="T">The response data type.</typeparam>
public record ApiResponse<T>
{
    /// <summary>Gets a value indicating whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the response data.</summary>
    public T? Data { get; init; }

    /// <summary>Gets the error message when the request fails.</summary>
    public string? Error { get; init; }

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T> { Success = true, Data = data };
    }

    /// <summary>Creates a failed response.</summary>
    public static ApiResponse<T> Fail(string error)
    {
        return new ApiResponse<T> { Success = false, Error = error };
    }
}
