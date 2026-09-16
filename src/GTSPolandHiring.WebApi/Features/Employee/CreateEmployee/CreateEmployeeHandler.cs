using GTSPolandHiring.WebApi.Features.Employee.Domain;
using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GTSPolandHiring.WebApi.Features.Employee.CreateEmployee;

public class CreateEmployeeCommandHandler(AppDbContext dbContext) 
    : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand command, CancellationToken ct)
    {

        var emailExists = await dbContext.Employees
            .AnyAsync(e => e.Email == command.Email, ct);

        if (emailExists)
        {
            return Result.Fail(new ConflictError(
                message: $"Employee with email '{command.Email}' already exists.",
                code: "EMPLOYEE_EMAIL_ALREADY_EXISTS"
            ));
        }

        if (!Enum.TryParse<EmployeeStatus>(command.Status, ignoreCase:true, out var status))
        {
            return Result.Fail(new ValidationError(
                message: $"Incorrect status: {command.Status}",
                code: "INCORRECT_EMPLOYEE_STATUS"
            ));
        }


        var employee = new Domain.Employee
        {
            Name = command.Name,
            HireDate = DateOnly.ParseExact(command.HireDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Email = command.Email,
            PhoneNo = command.PhoneNo,
            ProfilePicture = command.ProfilePicture,
            Status = status,
            Address = command.Address,
            State = command.State,
            Country = command.Country,
            City = command.City,
            Pincode = command.Pincode
        };


        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(employee.Id);
    }
}