using FluentValidation.TestHelper;
using GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.CreateEmployee;

public class CreateEmployeeCommandValidatorTests
{
    private const string AllowedDomain = "company.com";
    private const string CompanyFoundedDate = "1999-01-01";

    private readonly CreateEmployeeCommandValidator _validator;

    public CreateEmployeeCommandValidatorTests()
    {
        var options = Options.Create(new CompanyPolicyOptions
        {
            AllowedEmailDomain = AllowedDomain,
            CompanyFoundedDate = CompanyFoundedDate
        });

        _validator = new CreateEmployeeCommandValidator(options);
    }

    private static CreateEmployeeCommand ValidCommand(
        string? name = null,
        string? hireDate = null,
        string? email = null,
        string? phoneNo = null,
        string? profilePicture = null,
        string? status = null,
        string? address = null,
        string? state = null,
        string? country = null,
        string? city = null,
        string? pincode = null) => new(
            Name: name ?? "Jan Kowalski",
            HireDate: hireDate ?? "2024-01-15",
            Email: email ?? "jan.kowalski@company.com",
            PhoneNo: phoneNo ?? "+48123456789",
            ProfilePicture: profilePicture ?? "https://example.com/avatar.png",
            Status: status ?? "Active",
            Address: address ?? "Main Street 1",
            State: state ?? "Mazowieckie",
            Country: country ?? "Poland",
            City: city ?? "Warsaw",
            Pincode: pincode ?? "00-001");

    [Fact]
    public void Should_Pass_For_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(name: ""));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("EMPLOYEE_NAME_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Name_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(ValidCommand(name: new string('a', 101)));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("EMPLOYEE_NAME_TOO_LONG");
    }

    [Fact]
    public void Should_Fail_When_HireDate_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(hireDate: ""));

        result.ShouldHaveValidationErrorFor(x => x.HireDate)
            .WithErrorCode("EMPLOYEE_HIREDATE_REQUIRED");
    }

    [Theory]
    [InlineData("15-01-2024")]
    [InlineData("2024/01/15")]
    [InlineData("not-a-date")]
    public void Should_Fail_When_HireDate_Has_Invalid_Format(string hireDate)
    {
        var result = _validator.TestValidate(ValidCommand(hireDate: hireDate));

        result.ShouldHaveValidationErrorFor(x => x.HireDate)
            .WithErrorCode("EMPLOYEE_HIREDATE_INVALID_FORMAT");
    }

    [Fact]
    public void Should_Fail_When_HireDate_Is_In_The_Future()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd");

        var result = _validator.TestValidate(ValidCommand(hireDate: futureDate));

        result.ShouldHaveValidationErrorFor(x => x.HireDate)
            .WithErrorCode("EMPLOYEE_HIREDATE_CANNOT_BE_FUTURE");
    }

    [Fact]
    public void Should_Fail_When_HireDate_Is_Before_Company_Founded_Date()
    {
        var result = _validator.TestValidate(ValidCommand(hireDate: "1998-12-31"));

        result.ShouldHaveValidationErrorFor(x => x.HireDate)
            .WithErrorCode("EMPLOYEE_HIREDATE_CANNOT_BE_BEFORE_COMPANY_OPENED");
    }

    [Fact]
    public void Should_Pass_When_HireDate_Equals_Company_Founded_Date()
    {
        var result = _validator.TestValidate(ValidCommand(hireDate: CompanyFoundedDate));

        result.ShouldNotHaveValidationErrorFor(x => x.HireDate);
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(email: ""));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EMPLOYEE_EMAIL_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Malformed()
    {
        var result = _validator.TestValidate(ValidCommand(email: "not-an-email"));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EMPLOYEE_EMAIL_INVALID");
    }

    [Fact]
    public void Should_Fail_When_Email_Domain_Is_Not_Allowed()
    {
        var result = _validator.TestValidate(ValidCommand(email: "jan.kowalski@gmail.com"));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EMPLOYEE_EMAIL_INVALID_DOMAIN");
    }

    [Fact]
    public void Should_Fail_When_PhoneNo_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(phoneNo: ""));

        result.ShouldHaveValidationErrorFor(x => x.PhoneNo)
            .WithErrorCode("EMPLOYEE_PHONE_REQUIRED");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("+1-abc-1234")]
    [InlineData("0")]
    public void Should_Fail_When_PhoneNo_Has_Invalid_Format(string phoneNo)
    {
        var result = _validator.TestValidate(ValidCommand(phoneNo: phoneNo));

        result.ShouldHaveValidationErrorFor(x => x.PhoneNo)
            .WithErrorCode("EMPLOYEE_PHONE_INVALID");
    }

    [Theory]
    [InlineData("+1-555-0101")]
    [InlineData("+48 123 456 789")]
    [InlineData("(123) 456-7890")]
    public void Should_Pass_When_PhoneNo_Uses_Common_Country_Specific_Separators(string phoneNo)
    {
        var result = _validator.TestValidate(ValidCommand(phoneNo: phoneNo));

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNo);
    }

    [Fact]
    public void Should_Fail_When_ProfilePicture_Is_Not_Https()
    {
        var result = _validator.TestValidate(ValidCommand(profilePicture: "http://example.com/avatar.png"));

        result.ShouldHaveValidationErrorFor(x => x.ProfilePicture)
            .WithErrorCode("EMPLOYEE_PROFILE_PICTURE_NOT_SECURE");
    }

    [Fact]
    public void Should_Pass_When_ProfilePicture_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(profilePicture: ""));

        result.ShouldNotHaveValidationErrorFor(x => x.ProfilePicture);
    }

    [Fact]
    public void Should_Fail_When_Status_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(status: ""));

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorCode("EMPLOYEE_STATUS_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Status_Is_Not_A_Valid_Enum_Name()
    {
        var result = _validator.TestValidate(ValidCommand(status: "Retired"));

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorCode("EMPLOYEE_STATUS_INVALID");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("active")]
    [InlineData("Inactive")]
    public void Should_Pass_For_Valid_Status_Regardless_Of_Case(string status)
    {
        var result = _validator.TestValidate(ValidCommand(status: status));

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Should_Fail_When_Address_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(address: ""));

        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorCode("EMPLOYEE_ADDRESS_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_State_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(state: ""));

        result.ShouldHaveValidationErrorFor(x => x.State)
            .WithErrorCode("STATE_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_City_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(city: ""));

        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorCode("EMPLOYEE_CITY_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Country_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(country: ""));

        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorCode("EMPLOYEE_COUNTRY_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Pincode_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand(pincode: ""));

        result.ShouldHaveValidationErrorFor(x => x.Pincode)
            .WithErrorCode("EMPLOYEE_PINCODE_REQUIRED");
    }
}
