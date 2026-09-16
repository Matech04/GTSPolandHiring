using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Features.Employees.GetEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.GetEmployees;

public class GetEmployeesEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string Endpoint = "/api/v1/employees";

    private readonly GetEmployeesApiFactory _factory = new();
    private readonly HttpClient _client;

    public GetEmployeesEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    private async Task<Employee> SeedEmployeeAsync(string name, DateOnly createdAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employee = new Employee
        {
            Name = name,
            HireDate = new DateOnly(2024, 1, 15),
            Email = $"{Guid.NewGuid()}@company.com",
            PhoneNo = "+48123456789",
            ProfilePicture = "https://example.com/avatar.png",
            Status = EmployeeStatus.Active,
            Address = "Main Street 1",
            State = "Mazowieckie",
            Country = "Poland",
            City = "Warsaw",
            Pincode = "00-001",
            CreatedAt = createdAt
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        return employee;
    }

    [Fact]
    public async Task Get_Returns_200_And_Empty_List_When_No_Employees_Exist()
    {
        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<GetEmployeeResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body!);
    }

    [Fact]
    public async Task Get_Returns_200_And_All_Employees_Ordered_By_CreatedAt()
    {
        var second = await SeedEmployeeAsync("Anna Nowak", new DateOnly(2024, 2, 1));
        var first = await SeedEmployeeAsync("Jan Kowalski", new DateOnly(2024, 1, 1));

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<GetEmployeeResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        Assert.Equal(first.Id, body[0].Id);
        Assert.Equal(second.Id, body[1].Id);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
