using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.CreateEmployee;

public class CreateEmployeeEndpointTests : IDisposable
{
    private const string Endpoint = "/api/v1/employee";

    private readonly CreateEmployeeApiFactory _factory = new();
    private readonly HttpClient _client;

    public CreateEmployeeEndpointTests()
    {
        _client = _factory.CreateClient();
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
    public async Task Post_Returns_200_And_EmployeeId_For_Valid_Payload()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidCommand());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Post_Returns_400_With_ProblemDetails_When_Required_Field_Is_Missing()
    {
        var command = ValidCommand() with { Name = "" };

        var response = await _client.PostAsJsonAsync(Endpoint, command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("errorCode").GetString());

        var errorCodes = problem.GetProperty("errors")
            .EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToArray();
        Assert.Contains("EMPLOYEE_NAME_REQUIRED", errorCodes);
    }

    [Fact]
    public async Task Post_Returns_400_When_Email_Domain_Is_Not_Allowed()
    {
        var command = ValidCommand(email: "jan.kowalski@gmail.com");

        var response = await _client.PostAsJsonAsync(Endpoint, command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorCodes = problem.GetProperty("errors")
            .EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToArray();
        Assert.Contains("EMPLOYEE_EMAIL_INVALID_DOMAIN", errorCodes);
    }

    [Fact]
    public async Task Post_Returns_409_When_Email_Already_Exists()
    {
        var command = ValidCommand(email: "duplicate@company.com");

        var firstResponse = await _client.PostAsJsonAsync(Endpoint, command);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync(Endpoint, command);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS", problem.GetProperty("errorCode").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
