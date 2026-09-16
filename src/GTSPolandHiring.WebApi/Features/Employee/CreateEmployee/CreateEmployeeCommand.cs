using GTSPolandHiring.WebApi.Features.Employee.Domain;
using FluentResults;
using MediatR;

namespace GTSPolandHiring.WebApi.Features.Employee.CreateEmployee;
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