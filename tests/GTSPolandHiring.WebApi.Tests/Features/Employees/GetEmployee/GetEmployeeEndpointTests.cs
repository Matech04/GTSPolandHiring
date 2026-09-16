using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Features.Employees.GetEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.GetEmployee;

public class GetEmployeeEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly GetEmployeeApiFactory _factory = new();
    private readonly HttpClient _client;

    public GetEmployeeEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<Employee> SeedEmployeeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employee = new Employee
        {
            Name = "Jan Kowalski",
            HireDate = new DateOnly(2024, 1, 15),
            Email = $"{Guid.NewGuid()}@company.com",
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

    [Fact]
    public async Task Get_Returns_200_And_Employee_When_It_Exists()
    {
        var seeded = await SeedEmployeeAsync();

        var response = await _client.GetAsync($"/api/v1/employee/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetEmployeeResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(seeded.Id, body!.Id);
        Assert.Equal(seeded.Name, body.Name);
        Assert.Equal(seeded.Email, body.Email);
        Assert.Equal(seeded.HireDate, body.HireDate);
        Assert.Equal(EmployeeStatus.Active, body.Status);
    }

    [Fact]
    public async Task Get_Returns_404_With_ProblemDetails_When_Employee_Does_Not_Exist()
    {
        var response = await _client.GetAsync($"/api/v1/employee/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMPLOYEE_NOT_FOUND", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Get_Returns_404_When_Id_Is_Not_A_Valid_Guid()
    {
        var response = await _client.GetAsync("/api/v1/employee/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
