namespace GTSPolandHiring.WebApi.Features.Employees;

public interface IEmployeeInput
{
    string Name { get; }
    string HireDate { get; }
    string Email { get; }
    string PhoneNo { get; }
    string ProfilePicture { get; }
    string Status { get; }
    string Address { get; }
    string State { get; }
    string Country { get; }
    string City { get; }
    string Pincode { get; }
}
