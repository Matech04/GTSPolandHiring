using FluentResults;
using MediatR;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public record BulkImportEmployeesCommand(Stream FileStream) : IRequest<Result<BulkImportEmployeesResponse>>;
