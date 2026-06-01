#nullable enable

using System.Text.Json;
using SecureChat.Domain.Exceptions;

namespace SecureChat.Server.Middleware;

/// <summary>
/// Converts exceptions into consistent JSON API responses.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, ex, StatusCodes.Status401Unauthorized, "Unauthorized");
        }
        catch (DomainException ex)
        {
            await WriteErrorAsync(context, ex, ex.StatusCode, "DomainError");
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex, StatusCodes.Status500InternalServerError, "ServerError");
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception, int statusCode, string error)
    {
        _logger.LogError(exception, "Request failed with status code {StatusCode}.", statusCode);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            error,
            message = exception.Message,
            statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
