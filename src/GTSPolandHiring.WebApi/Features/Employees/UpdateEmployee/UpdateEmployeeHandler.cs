using System.Globalization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Features.Employees.GetEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;

public class UpdateEmployeeCommandHandler(AppDbContext dbContext)
    : IRequestHandler<UpdateEmployeeCommand, Result<GetEmployeeResponse>>
{
    public async Task<Result<GetEmployeeResponse>> Handle(UpdateEmployeeCommand command, CancellationToken ct)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(e => e.Id == command.Id, ct);

        if (employee is null)
        {
            return Result.Fail(new NotFoundError(
                message: $"Employee with id '{command.Id}' was not found.",
                code: "EMPLOYEE_NOT_FOUND"));
        }

        var emailTakenByAnotherEmployee = await dbContext.Employees
            .AnyAsync(e => e.Email == command.Email && e.Id != command.Id, ct);

        if (emailTakenByAnotherEmployee)
        {
            return Result.Fail(new ConflictError(
                message: $"Employee with email '{command.Email}' already exists.",
                code: "EMPLOYEE_EMAIL_ALREADY_EXISTS"));
        }

        employee.Name = command.Name;
        employee.HireDate = DateOnly.ParseExact(command.HireDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        employee.Email = command.Email;
        employee.PhoneNo = command.PhoneNo;
        employee.ProfilePicture = command.ProfilePicture;
        employee.Status = Enum.Parse<EmployeeStatus>(command.Status, ignoreCase: true);
        employee.Address = command.Address;
        employee.State = command.State;
        employee.Country = command.Country;
        employee.City = command.City;
        employee.Pincode = command.Pincode;

        await dbContext.SaveChangesAsync(ct);

        var response = new GetEmployeeResponse(
            employee.Id,
            employee.Name,
            employee.HireDate,
            employee.Email,
            employee.PhoneNo,
            employee.ProfilePicture,
            employee.Status,
            employee.Address,
            employee.State,
            employee.Country,
            employee.City,
            employee.Pincode,
            employee.CreatedAt);

        return Result.Ok(response);
    }
}
