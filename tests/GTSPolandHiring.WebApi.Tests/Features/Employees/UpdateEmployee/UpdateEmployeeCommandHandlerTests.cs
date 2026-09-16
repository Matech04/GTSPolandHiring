using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.UpdateEmployee;

public class UpdateEmployeeCommandHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly UpdateEmployeeCommandHandler _handler;

    public UpdateEmployeeCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _handler = new UpdateEmployeeCommandHandler(_dbContext);
    }

    private async Task<Employee> SeedEmployeeAsync(string email)
    {
        var employee = new Employee
        {
            Name = "Jan Kowalski",
            HireDate = new DateOnly(2024, 1, 15),
            Email = email,
            PhoneNo = "+48123456789",
            ProfilePicture = "https://example.com/avatar.png",
            Status = EmployeeStatus.Active,
            Address = "Main Street 1",
            State = "Mazowieckie",
            Country = "Poland",
            City = "Warsaw",
            Pincode = "00-001"
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        return employee;
    }

    private static UpdateEmployeeCommand ValidCommand(Guid id, string? email = null) => new(
        Id: id,
        Name: "Anna Nowak",
        HireDate: "2023-06-01",
        Email: email ?? "anna.nowak@company.com",
        PhoneNo: "+48987654321",
        ProfilePicture: "https://example.com/new-avatar.png",
        Status: "Inactive",
        Address: "Second Street 2",
        State: "Malopolskie",
        Country: "Poland",
        City: "Krakow",
        Pincode: "30-001");

    [Fact]
    public async Task Should_Update_Employee_And_Return_Updated_Response()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        var command = ValidCommand(seeded.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Id, result.Value.Id);
        Assert.Equal(command.Name, result.Value.Name);
        Assert.Equal(command.Email, result.Value.Email);
        Assert.Equal(EmployeeStatus.Inactive, result.Value.Status);

        var stored = await _dbContext.Employees.SingleAsync(e => e.Id == seeded.Id);
        Assert.Equal(command.Name, stored.Name);
        Assert.Equal(new DateOnly(2023, 6, 1), stored.HireDate);
        Assert.Equal(EmployeeStatus.Inactive, stored.Status);
    }

    [Fact]
    public async Task Should_Fail_When_Employee_Does_Not_Exist()
    {
        var command = ValidCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message.Contains("was not found"));
    }

    [Fact]
    public async Task Should_Fail_When_Email_Belongs_To_Another_Employee()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        await SeedEmployeeAsync("taken@company.com");

        var command = ValidCommand(seeded.Id, email: "taken@company.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));

        var stored = await _dbContext.Employees.SingleAsync(e => e.Id == seeded.Id);
        Assert.Equal("jan.kowalski@company.com", stored.Email);
    }

    [Fact]
    public async Task Should_Succeed_When_Email_Is_Unchanged_For_The_Same_Employee()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        var command = ValidCommand(seeded.Id, email: "jan.kowalski@company.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jan.kowalski@company.com", result.Value.Email);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
