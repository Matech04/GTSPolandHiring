using GTSPolandHiring.WebApi.Infrastructure.Errors;
using FluentValidation;

namespace GTSPolandHiring.WebApi.Features.Employee.CreateEmployee;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("EMPLOYEE_NAME_REQUIRED")
            .MaximumLength(100).WithErrorCode("EMPLOYEE_NAME_TOO_LONG");

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMPLOYEE_EMAIL_REQUIRED")
            .EmailAddress().WithErrorCode("EMPLOYEE_EMAIL_INVALID");

        RuleFor(x => x.PhoneNo)
            .NotEmpty().WithErrorCode("EMPLOYEE_PHONE_REQUIRED")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithErrorCode("EMPLOYEE_PHONE_INVALID");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithErrorCode("EMPLOYEE_HIREDATE_REQUIRED");

        RuleFor(x => x.Address)
            .NotEmpty().WithErrorCode("EMPLOYEE_ADDRESS_REQUIRED");

        RuleFor(x => x.City)
            .NotEmpty().WithErrorCode("EMPLOYEE_CITY_REQUIRED");

        RuleFor(x => x.Country)
            .NotEmpty().WithErrorCode("EMPLOYEE_COUNTRY_REQUIRED");

        RuleFor(x => x.Pincode)
            .NotEmpty().WithErrorCode("EMPLOYEE_PINCODE_REQUIRED");
    }
}