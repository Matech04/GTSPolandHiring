using GTSPolandHiring.WebApi.Features.Employees.Entities;

namespace GTSPolandHiring.WebApi.Features.Employees.GetEmployee;

public record GetEmployeeResponse(
    Guid Id,
    string Name,
    DateOnly HireDate,
    string Email,
    string PhoneNo,
    string ProfilePicture,
    EmployeeStatus Status,
    string Address,
    string State,
    string Country,
    string City,
    string Pincode,
    DateOnly CreatedAt);
