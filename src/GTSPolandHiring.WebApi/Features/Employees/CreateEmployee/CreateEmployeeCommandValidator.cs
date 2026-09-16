using GTSPolandHiring.WebApi.Infrastructure.Errors;
using FluentValidation;
using System.Globalization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Options;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator(IOptions<CompanyPolicyOptions> options)
    {

        var allowedEmailDomain = options.Value.AllowedEmailDomain;
        var companyFoundedDateString = options.Value.CompanyFoundedDate;

        DateOnly.TryParseExact(companyFoundedDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var companyFoundedDate);

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("EMPLOYEE_NAME_REQUIRED")
            .MaximumLength(100).WithErrorCode("EMPLOYEE_NAME_TOO_LONG");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithErrorCode("EMPLOYEE_HIREDATE_REQUIRED")
            .Custom((value, context) =>
            {
                if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    context.AddFailure(new ValidationFailure(context.PropertyPath, "HireDate must be in YYYY-MM-DD format.")
                    {
                        ErrorCode = "EMPLOYEE_HIREDATE_INVALID_FORMAT"
                    });
                    return;
                }

                if (date > DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    context.AddFailure(new ValidationFailure(context.PropertyPath, "HireDate cannot be in the future.")
                    {
                        ErrorCode = "EMPLOYEE_HIREDATE_CANNOT_BE_FUTURE"
                    });
                }
                else if (date < companyFoundedDate)
                {
                    context.AddFailure(new ValidationFailure(context.PropertyPath, "HireDate cannot be before company was created.")
                    {
                        ErrorCode = "EMPLOYEE_HIREDATE_CANNOT_BE_BEFORE_COMPANY_OPENED"
                    });
                }
            });

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMPLOYEE_EMAIL_REQUIRED")
            .EmailAddress().WithErrorCode("EMPLOYEE_EMAIL_INVALID")
            .Must(email => HaveAllowedDomain(email, allowedEmailDomain))
            .WithErrorCode("EMPLOYEE_EMAIL_INVALID_DOMAIN")
            .WithMessage($"Email must belong to the @{allowedEmailDomain} domain.");


        RuleFor(x => x.PhoneNo)
            .NotEmpty().WithErrorCode("EMPLOYEE_PHONE_REQUIRED")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithErrorCode("EMPLOYEE_PHONE_INVALID");

        RuleFor(x => x.ProfilePicture)
            .Must(BeSecureUrl)
            .WithErrorCode("EMPLOYEE_PROFILE_PICTURE_NOT_SECURE")
            .WithMessage("ProfilePicture URL must start with 'https://'.");

        RuleFor(x => x.Status)
            .NotEmpty().WithErrorCode("EMPLOYEE_STATUS_REQUIRED")
            .IsEnumName(typeof(EmployeeStatus), caseSensitive: false)
            .WithErrorCode("EMPLOYEE_STATUS_INVALID")
            .WithMessage("Status must be a valid EmployeeStatus value.");

        RuleFor(x => x.Address)
            .NotEmpty().WithErrorCode("EMPLOYEE_ADDRESS_REQUIRED");

        RuleFor(x => x.State)
             .NotEmpty().WithErrorCode("STATE_REQUIRED");

        RuleFor(x => x.City)
            .NotEmpty().WithErrorCode("EMPLOYEE_CITY_REQUIRED");

        RuleFor(x => x.Country)
            .NotEmpty().WithErrorCode("EMPLOYEE_COUNTRY_REQUIRED");

        RuleFor(x => x.Pincode)
            .NotEmpty().WithErrorCode("EMPLOYEE_PINCODE_REQUIRED");
    }


    private static bool HaveAllowedDomain(string email, string allowedDomain)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return false;
        var domain = email.Split('@')[1].ToLowerInvariant();
        return domain.Equals(allowedDomain.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool BeSecureUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}