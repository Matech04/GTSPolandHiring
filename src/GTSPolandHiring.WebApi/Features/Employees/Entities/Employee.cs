namespace GTSPolandHiring.WebApi.Features.Employees.Entities;

public class Employee
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNo { get; set; } = string.Empty;
    public string ProfilePicture { get; set; } = string.Empty; 
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    

    public string Address { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    
    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public enum EmployeeStatus
{
    Active,
    Inactive
}