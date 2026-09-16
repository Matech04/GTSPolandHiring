using GTSPolandHiring.WebApi.Features.Employees.Entities;
using FluentResults;
using MediatR;

namespace GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;
public record CreateEmployeeCommand(
    string Name,
    string HireDate,
    string Email,
    string PhoneNo,
    string ProfilePicture,
    string Status,
    string Address,
    string State,
    string Country,
    string City,
    string Pincode
) : IRequest<Result<Guid>>;