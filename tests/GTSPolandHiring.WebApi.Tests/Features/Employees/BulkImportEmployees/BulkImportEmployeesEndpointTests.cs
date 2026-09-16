using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GTSPolandHiring.WebApi.Tests.Features.Employees.BulkImportEmployees;

public class BulkImportEmployeesEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string Endpoint = "/api/v1/employees/bulk";
    private const string Header = "Name,Hiredate,Email,PhoneNo,ProfilePicture,Status,Address,State,Country,City,Pincode";

    private readonly BulkImportEmployeesApiFactory _factory = new();
    private readonly HttpClient _client;

    public BulkImportEmployeesEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    private static HttpContent BuildCsvContent(string csv, string fileName = "employees.csv")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Post_Imports_Valid_Rows_And_Reports_Failed_Rows_From_The_Real_Sample_Csv()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "TestData", "employees_sample.csv");
        var csvBytes = await File.ReadAllBytesAsync(csvPath);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(csvBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "employees_sample.csv");

        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(10, body.GetProperty("totalRows").GetInt32());
        Assert.Equal(6, body.GetProperty("imported").GetInt32());
        Assert.Equal(0, body.GetProperty("skipped").GetInt32());
        Assert.Equal(4, body.GetProperty("failed").GetInt32());

        var failedRows = body.GetProperty("issues")
            .EnumerateArray()
            .Select(i => i.GetProperty("row").GetInt32())
            .OrderBy(r => r)
            .ToArray();
        Assert.Equal([3, 8, 9, 11], failedRows);
    }

    [Fact]
    public async Task Post_Skips_Row_When_Email_Already_Exists_In_Database()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Employees.Add(new Employee
        {
            Name = "Existing Employee",
            Email = "existing@company.com",
            HireDate = new DateOnly(2020, 1, 1),
            PhoneNo = "+48123456789",
            ProfilePicture = "",
            Address = "Addr",
            State = "State",
            Country = "Country",
            City = "City",
            Pincode = "00-000"
        });
        await dbContext.SaveChangesAsync();

        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,existing@company.com,+48987654321,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        using var content = BuildCsvContent(csv);
        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(0, body.GetProperty("imported").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());

        var issue = body.GetProperty("issues").EnumerateArray().Single();
        Assert.Equal("Skipped", issue.GetProperty("status").GetString());
        Assert.Equal("EMPLOYEE_EMAIL_ALREADY_EXISTS",
            issue.GetProperty("errors").EnumerateArray().Single().GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Imports_First_And_Skips_Second_When_Two_Rows_Share_The_Same_New_Email()
    {
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,duplicate@company.com,+48987654321,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   Jan Kowalski,2024-01-02,duplicate@company.com,+48987654322,https://example.com/b.png,Active,Addr,State,Country,City,00-000
                   """;

        using var content = BuildCsvContent(csv);
        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, body.GetProperty("imported").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());

        var issue = body.GetProperty("issues").EnumerateArray().Single();
        Assert.Equal(3, issue.GetProperty("row").GetInt32());
        Assert.Equal("Skipped", issue.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_Reports_Failed_Row_With_Error_Codes_For_Invalid_Data()
    {
        var csv = $"""
                   {Header}
                   ,2024-01-01,bad-row@company.com,not-a-phone,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        using var content = BuildCsvContent(csv);
        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(0, body.GetProperty("imported").GetInt32());
        Assert.Equal(1, body.GetProperty("failed").GetInt32());

        var issue = body.GetProperty("issues").EnumerateArray().Single();
        var errorCodes = issue.GetProperty("errors")
            .EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToArray();
        Assert.Contains("EMPLOYEE_NAME_REQUIRED", errorCodes);
        Assert.Contains("EMPLOYEE_PHONE_INVALID", errorCodes);
    }

    [Fact]
    public async Task Post_Returns_400_When_File_Is_Empty()
    {
        using var content = BuildCsvContent("");

        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorCode = problem.GetProperty("errors").EnumerateArray().Single().GetProperty("code").GetString();
        Assert.Equal("EMPTY_FILE", errorCode);
    }

    [Fact]
    public async Task Post_Returns_415_When_File_Extension_Is_Not_Csv()
    {
        var csv = $"""
                   {Header}
                   Anna Nowak,2024-01-01,anna.nowak@company.com,+48987654321,https://example.com/a.png,Active,Addr,State,Country,City,00-000
                   """;

        using var content = BuildCsvContent(csv, fileName: "employees.txt");

        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("UNSUPPORTED_MEDIA_TYPE", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Post_Returns_413_When_File_Exceeds_Max_Size()
    {
        var oversizedRow = "Anna Nowak,2024-01-01,anna.nowak@company.com,+48987654321,https://example.com/a.png,Active,"
            + new string('a', 6 * 1024 * 1024) + ",State,Country,City,00-000";
        var csv = $"{Header}\n{oversizedRow}";

        using var content = BuildCsvContent(csv);

        var response = await _client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("FILE_TOO_LARGE", problem.GetProperty("errorCode").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
