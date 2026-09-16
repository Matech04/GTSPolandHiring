using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using GTSPolandHiring.WebApi.Features.Employee.CreateEmployee;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1,0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1,0))
    .Build();

var v1Group = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

v1Group.MapCreateEmployeeEndpoint();

app.UseHttpsRedirection();

app.Run();
