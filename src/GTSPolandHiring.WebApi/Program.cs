using System.Globalization;
using Asp.Versioning;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;
using GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;
using GTSPolandHiring.WebApi.Features.Employees.DeleteEmployee;
using GTSPolandHiring.WebApi.Features.Employees.GetEmployee;
using GTSPolandHiring.WebApi.Features.Employees.GetEmployees;
using GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;
using GTSPolandHiring.WebApi.Infrastructure.Behaviors;
using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Options;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddOptions<CompanyPolicyOptions>()
    .Bind(builder.Configuration.GetSection(CompanyPolicyOptions.SectionName))
    .Validate(o => DateOnly.TryParseExact(o.CompanyFoundedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        "CompanyFoundedDate must be in yyyy-MM-dd format")
    .ValidateOnStart();

builder.Services.AddOptions<BulkImportOptions>()
    .Bind(builder.Configuration.GetSection(BulkImportOptions.SectionName))
    .Validate(o => o.MaxFileSizeBytes > 0, "MaxFileSizeBytes must be greater than zero.")
    .ValidateOnStart();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()
    );
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

builder.Services.AddRequestTimeouts();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Tests swap in the InMemory provider, which doesn't support migrations at all — only
    // apply them when we're actually talking to a real relational database.
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference();
}

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .Build();

var v1Group = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

v1Group.MapCreateEmployeeEndpoint();
v1Group.MapGetEmployeeEndpoint();
v1Group.MapGetEmployeesEndpoint();
v1Group.MapUpdateEmployeeEndpoint();
v1Group.MapDeleteEmployeeEndpoint();
v1Group.MapBulkImportEmployeesEndpoint();

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseRequestTimeouts();

var supportedCultures = new[] { "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.Run();
