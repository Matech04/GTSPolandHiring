using FluentResults;
using GTSPolandHiring.WebApi.Infrastructure.Errors;
using Microsoft.AspNetCore.Http;

namespace GTSPolandHiring.WebApi.Infrastructure.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess != null
                ? onSuccess(result.Value)
                : Results.Ok(result.Value);
        }

        return MapErrorsToProblemDetails(result.Errors);
    }

    public static IResult ToHttpResult(this Result result, IResult? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess ?? Results.Ok();
        }

        return MapErrorsToProblemDetails(result.Errors);
    }

    private static IResult MapErrorsToProblemDetails(IReadOnlyList<IError> errors)
    {

        static string GetErrorCode(IError error) =>
            error.Metadata.TryGetValue("ErrorCode", out var code) && code is string codeStr
                ? codeStr
                : "UNSPECIFIED_ERROR";

        // 1. 404 Not Found
        var notFoundError = errors.FirstOrDefault(e => e is NotFoundError || e.HasMetadataKey("NotFound"));
        if (notFoundError is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: notFoundError.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", GetErrorCode(notFoundError) }
                }
            );
        }

        // 2. 409 Conflict
        var conflictError = errors.FirstOrDefault(e => e is ConflictError || e.HasMetadataKey("Conflict"));
        if (conflictError is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: conflictError.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", GetErrorCode(conflictError) }
                }
            );
        }

        // 3. 413 Payload / Content Too Large (np. zbyt duży plik CSV)
        var fileTooLargeError = errors.FirstOrDefault(e => e is FileTooLargeError || e.HasMetadataKey("ContentTooLarge"));
        if (fileTooLargeError is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Content Too Large",
                detail: fileTooLargeError.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", GetErrorCode(fileTooLargeError) }
                }
            );
        }

        // 4. 415 Unsupported Media Type (np. zły format pliku)
        var mediaTypeError = errors.FirstOrDefault(e => e is UnsupportedMediaTypeError || e.HasMetadataKey("UnsupportedMediaType"));
        if (mediaTypeError is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType,
                title: "Unsupported Media Type",
                detail: mediaTypeError.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "errorCode", GetErrorCode(mediaTypeError) }
                }
            );
        }

        // 5. 400 Bad Request
        var formattedErrors = errors.Select(e => new
        {
            code = GetErrorCode(e),
            message = e.Message
        }).ToArray();

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: "One or more validation or business errors occurred.",
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", "VALIDATION_FAILED" },
                { "errors", formattedErrors }
            }
        );
    }
}