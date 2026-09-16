using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Extensions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public static class BulkImportEmployeesEndpoint
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = [".csv"];

    public static void MapBulkImportEmployeesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/employees/bulk", async (
            IFormFile file,
            ISender mediator,
            CancellationToken ct) =>
            {
                if (file.Length == 0)
                {
                    return Result.Fail(new ValidationError(
                        message: "The uploaded file is empty.",
                        code: "EMPTY_FILE")).ToHttpResult();
                }

                if (file.Length > MaxFileSizeBytes)
                {
                    return Result.Fail(new FileTooLargeError(MaxFileSizeBytes)).ToHttpResult();
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return Result.Fail(new UnsupportedMediaTypeError(string.Join(", ", AllowedExtensions))).ToHttpResult();
                }

                await using var stream = file.OpenReadStream();
                var result = await mediator.Send(new BulkImportEmployeesCommand(stream), ct);

                return result.ToHttpResult();
            })
            .DisableAntiforgery();
    }
}
