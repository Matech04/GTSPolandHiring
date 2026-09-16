using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Extensions;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Features.Employees.GetEmployee;

public static class GetEmployeeEndpoint
{
    public static void MapGetEmployeeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/employee/{id:guid}", async (
            [FromRoute] Guid id,
            AppDbContext dbContext,
            CancellationToken ct) =>
            {
                var employee = await dbContext.Employees
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => new GetEmployeeResponse(
                        e.Id,
                        e.Name,
                        e.HireDate,
                        e.Email,
                        e.PhoneNo,
                        e.ProfilePicture,
                        e.Status,
                        e.Address,
                        e.State,
                        e.Country,
                        e.City,
                        e.Pincode,
                        e.CreatedAt))
                    .SingleOrDefaultAsync(ct);

                var result = employee is not null
                    ? Result.Ok(employee)
                    : Result.Fail<GetEmployeeResponse>(new NotFoundError(
                        message: $"Employee with id '{id}' was not found.",
                        code: "EMPLOYEE_NOT_FOUND"));

                return result.ToHttpResult();
            });
    }
}
