#nullable enable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SecureChat.Server.Filters;

/// <summary>
/// Returns a 400 response when model validation fails.
/// </summary>
public class ValidationFilter : ActionFilterAttribute
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        context.Result = new BadRequestObjectResult(new
        {
            error = "ValidationError",
            message = "The request payload is invalid.",
            statusCode = StatusCodes.Status400BadRequest,
            errors
        });
    }
}
