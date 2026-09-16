using GTSPolandHiring.WebApi.Features.Employees.GetEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Features.Employees.GetEmployees;

public static class GetEmployeesEndpoint
{
    public static void MapGetEmployeesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/employees", async (
            AppDbContext dbContext,
            CancellationToken ct) =>
            {
                var employees = await dbContext.Employees
                    .AsNoTracking()
                    .OrderBy(e => e.CreatedAt)
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
                    .ToListAsync(ct);

                return Results.Ok(employees);
            });
    }
}
