using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.UpdateEmployee;

public class UpdateEmployeeEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly UpdateEmployeeApiFactory _factory = new();
    private readonly HttpClient _client;

    public UpdateEmployeeEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<Employee> SeedEmployeeAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

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
    public async Task Put_Returns_200_And_Updated_Employee_For_Valid_Payload()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        var command = ValidCommand(seeded.Id);

        var response = await _client.PutAsJsonAsync($"/api/v1/employee/{seeded.Id}", command);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(command.Name, body.GetProperty("name").GetString());
        Assert.Equal(command.Email, body.GetProperty("email").GetString());
        Assert.Equal("Inactive", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Put_Ignores_Id_Mismatch_Between_Route_And_Body()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        var command = ValidCommand(Guid.NewGuid());

        var response = await _client.PutAsJsonAsync($"/api/v1/employee/{seeded.Id}", command);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(seeded.Id.ToString(), body.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Put_Returns_404_When_Employee_Does_Not_Exist()
    {
        var command = ValidCommand(Guid.NewGuid());

        var response = await _client.PutAsJsonAsync($"/api/v1/employee/{command.Id}", command);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMPLOYEE_NOT_FOUND", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Put_Returns_400_When_Required_Field_Is_Missing()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        var command = ValidCommand(seeded.Id) with { Name = "" };

        var response = await _client.PutAsJsonAsync($"/api/v1/employee/{seeded.Id}", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorCodes = problem.GetProperty("errors")
            .EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToArray();
        Assert.Contains("EMPLOYEE_NAME_REQUIRED", errorCodes);
    }

    [Fact]
    public async Task Put_Returns_409_When_Email_Belongs_To_Another_Employee()
    {
        var seeded = await SeedEmployeeAsync("jan.kowalski@company.com");
        await SeedEmployeeAsync("taken@company.com");

        var command = ValidCommand(seeded.Id, email: "taken@company.com");

        var response = await _client.PutAsJsonAsync($"/api/v1/employee/{seeded.Id}", command);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS", problem.GetProperty("errorCode").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
