namespace GTSPolandHiring.WebApi.Infrastructure.Options;

public class BulkImportOptions
{
    public const string SectionName = "BulkImport";
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
}
