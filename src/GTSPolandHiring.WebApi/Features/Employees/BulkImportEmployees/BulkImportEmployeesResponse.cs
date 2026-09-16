namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public record BulkImportEmployeesResponse(
    int TotalRows,
    int Imported,
    int Skipped,
    int Failed,
    IReadOnlyList<BulkImportIssue> Issues);

public record BulkImportIssue(
    int Row,
    string? Email,
    BulkImportRowStatus Status,
    IReadOnlyList<BulkImportError> Errors);

public record BulkImportError(string Code, string Message);

public enum BulkImportRowStatus
{
    Skipped,
    Failed
}
