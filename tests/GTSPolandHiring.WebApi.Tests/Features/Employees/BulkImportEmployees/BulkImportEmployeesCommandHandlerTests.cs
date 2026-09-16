using System.Text;
using GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Options;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using GTSPolandHiring.WebApi.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.BulkImportEmployees;

public class BulkImportEmployeesCommandHandlerTests : IDisposable
{
    private const string Header = "Name,Hiredate,Email,PhoneNo,ProfilePicture,Status,Address,State,Country,City,Pincode";

    private readonly AppDbContext _dbContext;
    private readonly BulkImportEmployeesCommandHandler _handler;

    public BulkImportEmployeesCommandHandlerTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(dbOptions);

        var companyPolicyOptions = Options.Create(new CompanyPolicyOptions
        {
            AllowedEmailDomain = "company.com",
            CompanyFoundedDate = "1999-01-01"
        });
        var rowValidator = new BulkImportEmployeeRowValidator(companyPolicyOptions);

        _handler = new BulkImportEmployeesCommandHandler(_dbContext, rowValidator);
    }

    private static Stream ToStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    private async Task SeedEmployeeAsync(string email)
    {
        _dbContext.Employees.Add(new Employee
        {
            Name = "Existing Employee",
            Email = email,
            HireDate = new DateOnly(2020, 1, 1),
            PhoneNo = "+48123456789",
            ProfilePicture = "",
            Address = "Addr",
            State = "State",
            Country = "Country",
            City = "City",
            Pincode = "00-000"
        });
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_Import_A_Valid_Row_And_Persist_It()
    {
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,anna.nowak@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRows);
        Assert.Equal(1, result.Value.Imported);
        Assert.Equal(0, result.Value.Skipped);
        Assert.Equal(0, result.Value.Failed);
        Assert.Empty(result.Value.Issues);

        var stored = await _dbContext.Employees.SingleAsync();
        Assert.Equal("anna.nowak@company.com", stored.Email);
        Assert.Equal(EmployeeStatus.Active, stored.Status);
    }

    [Fact]
    public async Task Should_Fail_Row_With_Validation_Errors_And_Not_Persist_It()
    {
        var csv = $"""
                   {Header}
                   ,2024-01-01,bad-row@company.com,not-a-phone,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Imported);
        Assert.Equal(1, result.Value.Failed);

        var issue = result.Value.Issues.Single();
        Assert.Equal(BulkImportRowStatus.Failed, issue.Status);
        Assert.Contains(issue.Errors, e => e.Code == "EMPLOYEE_NAME_REQUIRED");
        Assert.Contains(issue.Errors, e => e.Code == "EMPLOYEE_PHONE_INVALID");

        Assert.Equal(0, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Skip_Row_When_Email_Already_Exists_In_Database()
    {
        await SeedEmployeeAsync("existing@company.com");

        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,existing@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.Equal(0, result.Value.Imported);
        Assert.Equal(1, result.Value.Skipped);

        var issue = result.Value.Issues.Single();
        Assert.Equal(BulkImportRowStatus.Skipped, issue.Status);
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS", issue.Errors.Single().Code);

        Assert.Equal(1, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Import_First_And_Skip_Second_Row_For_Duplicate_Email_Within_The_Same_File()
    {
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,duplicate@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   Jan Kowalski,2024-01-02,duplicate@company.com,+48123456780,https://example.com/b.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.Equal(1, result.Value.Imported);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(1, await _dbContext.Employees.CountAsync());

        var skippedIssue = result.Value.Issues.Single(i => i.Status == BulkImportRowStatus.Skipped);
        Assert.Equal(3, skippedIssue.Row);
    }

    [Fact]
    public async Task Should_Import_Correctly_Even_When_Unrelated_Employees_Already_Exist()
    {
        for (var i = 0; i < 20; i++)
        {
            await SeedEmployeeAsync($"unrelated{i}@company.com");
        }

        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,anna.nowak@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.Equal(1, result.Value.Imported);
        Assert.Equal(0, result.Value.Skipped);
        Assert.Equal(21, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Fail_When_File_Has_No_Readable_Header()
    {
        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream("")), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e =>
            e.Metadata.TryGetValue("ErrorCode", out var code) && (string)code == "EMPTY_FILE");
    }

    [Fact]
    public async Task Should_Fail_Row_When_Address_Exceeds_The_Column_Length_Instead_Of_Crashing_The_Import()
    {
        // Regression test: before the row validator mirrored the column's MaxLength, this row
        // would pass validation and only fail on SaveChangesAsync, taking the whole batch down
        // with an unhandled DbUpdateException instead of a clean, isolated row failure.
        var overlongAddress = new string('a', 256);
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,anna.nowak@company.com,+48123456789,https://example.com/a.png,Active,{overlongAddress},State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Imported);
        Assert.Equal(1, result.Value.Failed);
        Assert.Equal("EMPLOYEE_ADDRESS_TOO_LONG", result.Value.Issues.Single().Errors.Single().Code);
        Assert.Equal(0, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Skip_Row_When_Existing_Email_Differs_Only_By_Case()
    {
        await SeedEmployeeAsync("Existing@Company.com");

        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,existing@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.Equal(0, result.Value.Imported);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS", result.Value.Issues.Single().Errors.Single().Code);
        Assert.Equal(1, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Import_First_And_Skip_Second_Row_For_Duplicate_Email_With_Different_Casing()
    {
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,Duplicate@Company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   Jan Kowalski,2024-01-02,duplicate@company.com,+48123456780,https://example.com/b.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await _handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.Equal(1, result.Value.Imported);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(1, await _dbContext.Employees.CountAsync());
    }

    [Fact]
    public async Task Should_Recover_Via_Per_Row_Retry_When_The_Batch_Save_Loses_A_Race_On_One_Email()
    {
        // Simulates another request inserting "jan.kowalski@company.com" at the exact moment our
        // batch SaveChangesAsync runs, after our own pre-check already found it free. The handler
        // must not let that single collision take down the whole import: Anna should still be
        // imported, and only Jan's row should come back as a skipped conflict.
        //
        // The InMemory provider does not actually enforce unique indexes (verified separately),
        // so the interceptor below stands in for Postgres's real unique-constraint violation: it
        // injects the racing row once and then throws for every save that touches the conflicting
        // email, exactly as a real duplicate-key error would on each retry.
        var dbName = Guid.NewGuid().ToString();

        var interceptor = new ConflictSimulatingInterceptor(dbName, "jan.kowalski@company.com");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new AppDbContext(options);
        var rowValidator = new BulkImportEmployeeRowValidator(Options.Create(new CompanyPolicyOptions
        {
            AllowedEmailDomain = "company.com",
            CompanyFoundedDate = "1999-01-01"
        }));
        var handler = new BulkImportEmployeesCommandHandler(dbContext, rowValidator);

        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,anna.nowak@company.com,+48123456789,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   Jan Kowalski,2024-01-02,jan.kowalski@company.com,+48123456780,https://example.com/b.png,Active,Addr,State,Country,City,00-000
                   """;

        var result = await handler.Handle(new BulkImportEmployeesCommand(ToStream(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Imported);
        Assert.Equal(1, result.Value.Skipped);

        var issue = result.Value.Issues.Single();
        Assert.Equal("jan.kowalski@company.com", issue.Email);
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS", issue.Errors.Single().Code);

        Assert.Equal(2, await dbContext.Employees.CountAsync()); // the "racing" insert + Anna
        Assert.True(await dbContext.Employees.AnyAsync(e => e.Email == "anna.nowak@company.com"));
    }

    public void Dispose() => _dbContext.Dispose();
}
