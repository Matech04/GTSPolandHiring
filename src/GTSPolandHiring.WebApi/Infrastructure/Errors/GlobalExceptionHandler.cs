using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GTSPolandHiring.WebApi.Infrastructure.Errors;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken ct)
    {
        logger.LogError(
            exception, 
            "An unhandled exception occurred while processing request on {Path}", 
            httpContext.Request.Path
        );

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred on the server.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = "INTERNAL_SERVER_ERROR";

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true; 
    }
}