using GTSPolandHiring.WebApi.Infrastructure.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;

public static class UpdateEmployeeEndpoint
{
    public static void MapUpdateEmployeeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/employee/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateEmployeeCommand command,
            [FromServices] ISender mediator,
            CancellationToken ct) =>
            {
                var result = await mediator.Send(command with { Id = id }, ct);

                return result.ToHttpResult();
            });
    }
}
