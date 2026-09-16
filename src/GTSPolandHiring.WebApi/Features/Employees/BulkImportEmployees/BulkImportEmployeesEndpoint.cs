using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Extensions;
using GTSPolandHiring.WebApi.Infrastructure.Options;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public static class BulkImportEmployeesEndpoint
{
    private static readonly string[] AllowedExtensions = [".csv"];
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromSeconds(30);

    public static void MapBulkImportEmployeesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/employees/bulk", async (
            IFormFile file,
            ISender mediator,
            IOptions<BulkImportOptions> bulkImportOptions,
            CancellationToken ct) =>
            {
                if (file.Length == 0)
                {
                    return Result.Fail(new ValidationError(
                        message: "The uploaded file is empty.",
                        code: "EMPTY_FILE")).ToHttpResult();
                }

                var maxFileSizeBytes = bulkImportOptions.Value.MaxFileSizeBytes;
                if (file.Length > maxFileSizeBytes)
                {
                    return Result.Fail(new FileTooLargeError(maxFileSizeBytes)).ToHttpResult();
                }

                var extension = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return Result.Fail(new UnsupportedMediaTypeError(string.Join(", ", AllowedExtensions))).ToHttpResult();
                }

                await using var stream = file.OpenReadStream();

                try
                {
                    var result = await mediator.Send(new BulkImportEmployeesCommand(stream), ct);
                    return result.ToHttpResult();
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return Result.Fail(new ValidationError(
                        message: "The file could not be fully processed in time. It may be malformed or too complex to parse.",
                        code: "CSV_PROCESSING_TIMEOUT")).ToHttpResult();
                }
            })
            .WithRequestTimeout(ProcessingTimeout)
            .DisableAntiforgery();
    }
}
