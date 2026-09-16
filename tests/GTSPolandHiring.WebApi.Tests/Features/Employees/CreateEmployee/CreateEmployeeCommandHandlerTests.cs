using GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.CreateEmployee;

public class CreateEmployeeCommandHandlerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly CreateEmployeeCommandHandler _handler;

    public CreateEmployeeCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _handler = new CreateEmployeeCommandHandler(_dbContext);
    }

    private static CreateEmployeeCommand ValidCommand(string? email = null) => new(
        Name: "Jan Kowalski",
        HireDate: "2024-01-15",
        Email: email ?? "jan.kowalski@company.com",
        PhoneNo: "+48123456789",
        ProfilePicture: "https://example.com/avatar.png",
        Status: "Active",
        Address: "Main Street 1",
        State: "Mazowieckie",
        Country: "Poland",
        City: "Warsaw",
        Pincode: "00-001");

    [Fact]
    public async Task Should_Create_Employee_And_Return_Its_Id()
    {
        var command = ValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var stored = await _dbContext.Employees.SingleAsync(e => e.Id == result.Value);
        Assert.Equal(command.Name, stored.Name);
        Assert.Equal(command.Email, stored.Email);
        Assert.Equal(new DateOnly(2024, 1, 15), stored.HireDate);
        Assert.Equal(EmployeeStatus.Active, stored.Status);
    }

    [Fact]
    public async Task Should_Fail_When_Email_Already_Exists()
    {
        var existingEmployee = new Employee
        {
            Name = "Existing Employee",
            Email = "jan.kowalski@company.com",
            HireDate = new DateOnly(2020, 1, 1)
        };
        _dbContext.Employees.Add(existingEmployee);
        await _dbContext.SaveChangesAsync();

        var command = ValidCommand(email: "jan.kowalski@company.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));

        var employeeCount = await _dbContext.Employees.CountAsync();
        Assert.Equal(1, employeeCount);
    }

    [Fact]
    public async Task Should_Parse_Status_Case_Insensitively()
    {
        var command = ValidCommand() with { Status = "inactive" };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _dbContext.Employees.SingleAsync(e => e.Id == result.Value);
        Assert.Equal(EmployeeStatus.Inactive, stored.Status);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
