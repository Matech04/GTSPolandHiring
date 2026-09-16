using CsvHelper.Configuration.Attributes;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public class BulkImportEmployeeRow : IEmployeeInput
{
    [Name("Name")]
    public string Name { get; set; } = string.Empty;

    [Name("Hiredate")]
    public string HireDate { get; set; } = string.Empty;

    [Name("Email")]
    public string Email { get; set; } = string.Empty;

    [Name("PhoneNo")]
    public string PhoneNo { get; set; } = string.Empty;

    [Name("ProfilePicture")]
    public string ProfilePicture { get; set; } = string.Empty;

    [Name("Status")]
    public string Status { get; set; } = string.Empty;

    [Name("Address")]
    public string Address { get; set; } = string.Empty;

    [Name("State")]
    public string State { get; set; } = string.Empty;

    [Name("Country")]
    public string Country { get; set; } = string.Empty;

    [Name("City")]
    public string City { get; set; } = string.Empty;

    [Name("Pincode")]
    public string Pincode { get; set; } = string.Empty;
}
