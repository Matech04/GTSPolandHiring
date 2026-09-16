using System.Globalization;
using GTSPolandHiring.WebApi.Features.Employees.Entities;
using GTSPolandHiring.WebApi.Infrastructure.Errors;
using GTSPolandHiring.WebApi.Infrastructure.Persistence;
using CsvHelper;
using CsvHelper.Configuration;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public class BulkImportEmployeesCommandHandler(AppDbContext dbContext, IValidator<BulkImportEmployeeRow> rowValidator)
    : IRequestHandler<BulkImportEmployeesCommand, Result<BulkImportEmployeesResponse>>
{
    public async Task<Result<BulkImportEmployeesResponse>> Handle(BulkImportEmployeesCommand command, CancellationToken ct)
    {
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(command.FileStream);
        using var csv = new CsvReader(reader, csvConfig);

        bool hasHeader;
        try
        {
            hasHeader = await csv.ReadAsync() && csv.ReadHeader();
        }
        catch (CsvHelperException)
        {
            hasHeader = false;
        }

        if (!hasHeader)
        {
            return Result.Fail(new ValidationError(
                message: "The uploaded file has no readable CSV header row.",
                code: "EMPTY_FILE"));
        }

        var existingEmails = (await dbContext.Employees.Select(e => e.Email).ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        var issues = new List<BulkImportIssue>();
        var newEmployees = new List<Employee>();
        var rowNumber = 1; // row 1 is the header

        while (true)
        {
            bool hasRecord;
            try
            {
                hasRecord = await csv.ReadAsync();
            }
            catch (CsvHelperException ex)
            {
                rowNumber++;
                issues.Add(new BulkImportIssue(rowNumber, null, BulkImportRowStatus.Failed,
                    [new BulkImportError("MALFORMED_ROW", ex.Message)]));
                continue;
            }

            if (!hasRecord)
            {
                break;
            }

            rowNumber++;

            BulkImportEmployeeRow row;
            try
            {
                row = csv.GetRecord<BulkImportEmployeeRow>();
            }
            catch (CsvHelperException ex)
            {
                issues.Add(new BulkImportIssue(rowNumber, null, BulkImportRowStatus.Failed,
                    [new BulkImportError("MALFORMED_ROW", ex.Message)]));
                continue;
            }

            var validationResult = await rowValidator.ValidateAsync(row, ct);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new BulkImportError(
                        string.IsNullOrEmpty(e.ErrorCode) ? "VALIDATION_ERROR" : e.ErrorCode,
                        e.ErrorMessage))
                    .ToList();

                issues.Add(new BulkImportIssue(rowNumber, row.Email, BulkImportRowStatus.Failed, errors));
                continue;
            }

            if (existingEmails.Contains(row.Email))
            {
                issues.Add(new BulkImportIssue(rowNumber, row.Email, BulkImportRowStatus.Skipped,
                    [new BulkImportError("EMPLOYEE_EMAIL_ALREADY_EXISTS", $"Employee with email '{row.Email}' already exists.")]));
                continue;
            }

            newEmployees.Add(new Employee
            {
                Name = row.Name,
                HireDate = DateOnly.ParseExact(row.HireDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Email = row.Email,
                PhoneNo = row.PhoneNo,
                ProfilePicture = row.ProfilePicture,
                Status = Enum.Parse<EmployeeStatus>(row.Status, ignoreCase: true),
                Address = row.Address,
                State = row.State,
                Country = row.Country,
                City = row.City,
                Pincode = row.Pincode
            });

            existingEmails.Add(row.Email);
        }

        if (newEmployees.Count > 0)
        {
            dbContext.Employees.AddRange(newEmployees);
            await dbContext.SaveChangesAsync(ct);
        }

        var response = new BulkImportEmployeesResponse(
            TotalRows: rowNumber - 1,
            Imported: newEmployees.Count,
            Skipped: issues.Count(i => i.Status == BulkImportRowStatus.Skipped),
            Failed: issues.Count(i => i.Status == BulkImportRowStatus.Failed),
            Issues: issues);

        return Result.Ok(response);
    }
}
