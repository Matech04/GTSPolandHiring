using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Extensions;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Features.Employees.DeleteEmployee;

public static class DeleteEmployeeEndpoint
{
    public static void MapDeleteEmployeeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/employee/{id:guid}", async (
            [FromRoute] Guid id,
            AppDbContext dbContext,
            CancellationToken ct) =>
            {
                var employee = await dbContext.Employees.SingleOrDefaultAsync(e => e.Id == id, ct);

                if (employee is null)
                {
                    return Result.Fail(new NotFoundError(
                        message: $"Employee with id '{id}' was not found.",
                        code: "EMPLOYEE_NOT_FOUND")).ToHttpResult();
                }

                dbContext.Employees.Remove(employee);
                await dbContext.SaveChangesAsync(ct);

                return Results.NoContent();
            });
    }
}
