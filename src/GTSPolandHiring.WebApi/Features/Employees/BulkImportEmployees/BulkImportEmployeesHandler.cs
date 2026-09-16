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

        var issues = new List<BulkImportIssue>();
        var validRows = new List<(int RowNumber, BulkImportEmployeeRow Row)>();
        var rowNumber = 1; // row 1 is the header

        while (true)
        {

            ct.ThrowIfCancellationRequested();

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

            validRows.Add((rowNumber, row));
        }

        var csvEmailsLower = validRows.Select(r => r.Row.Email.ToLowerInvariant()).Distinct().ToList();
        var existingEmails = (await dbContext.Employees
                .Where(e => csvEmailsLower.Contains(e.Email.ToLower()))
                .Select(e => e.Email.ToLower())
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        var newEmployees = new List<(int RowNumber, Employee Employee)>();
        var seenInFile = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (currentRowNumber, row) in validRows)
        {
            var normalizedEmail = row.Email.ToLowerInvariant();
            if (existingEmails.Contains(normalizedEmail) || !seenInFile.Add(normalizedEmail))
            {
                issues.Add(new BulkImportIssue(currentRowNumber, row.Email, BulkImportRowStatus.Skipped,
                    [new BulkImportError("EMPLOYEE_EMAIL_ALREADY_EXISTS", $"Employee with email '{row.Email}' already exists.")]));
                continue;
            }

            newEmployees.Add((currentRowNumber, new Employee
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
            }));
        }

        var importedCount = 0;
        if (newEmployees.Count > 0)
        {
            importedCount = await SaveNewEmployeesAsync(dbContext, newEmployees, issues, ct);
        }

        var response = new BulkImportEmployeesResponse(
            TotalRows: rowNumber - 1,
            Imported: importedCount,
            Skipped: issues.Count(i => i.Status == BulkImportRowStatus.Skipped),
            Failed: issues.Count(i => i.Status == BulkImportRowStatus.Failed),
            Issues: issues);

        return Result.Ok(response);
    }

    private static async Task<int> SaveNewEmployeesAsync(
        AppDbContext dbContext,
        List<(int RowNumber, Employee Employee)> newEmployees,
        List<BulkImportIssue> issues,
        CancellationToken ct)
    {
        dbContext.Employees.AddRange(newEmployees.Select(e => e.Employee));

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return newEmployees.Count;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
        }

        var imported = 0;
        foreach (var (rowNumber, employee) in newEmployees)
        {
            dbContext.Employees.Add(employee);
            try
            {
                await dbContext.SaveChangesAsync(ct);
                imported++;
            }
            catch (DbUpdateException)
            {
                dbContext.Entry(employee).State = EntityState.Detached;

                var stillConflicts = await dbContext.Employees
                    .AnyAsync(e => e.Email.ToLower() == employee.Email.ToLower(), ct);

                issues.Add(stillConflicts
                    ? new BulkImportIssue(rowNumber, employee.Email, BulkImportRowStatus.Skipped,
                        [new BulkImportError("EMPLOYEE_EMAIL_ALREADY_EXISTS", $"Employee with email '{employee.Email}' already exists.")])
                    : new BulkImportIssue(rowNumber, employee.Email, BulkImportRowStatus.Failed,
                        [new BulkImportError("EMPLOYEE_SAVE_FAILED", "The employee record could not be saved due to a database error.")]));
            }
        }

        return imported;
    }
}
