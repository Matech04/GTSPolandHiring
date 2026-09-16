using Microsoft.AspNetCore.Mvc;
using MediatR;
using GTSPolandHiring.WebApi.Infrastructure.Extensions;

namespace GTSPolandHiring.WebApi.Features.Employee.CreateEmployee;

public static class CreateEmployeeEndpoint
{
    public static void MapCreateEmployeeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/employee", async (
            [FromBody] CreateEmployeeCommand command,
            [FromServices] ISender mediator,
            CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);

                return result.ToHttpResult();

            });
    }
}