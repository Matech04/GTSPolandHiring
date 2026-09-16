namespace GTSPolandHiring.WebApi.Infrastructure.Options;

public class CompanyPolicyOptions
{
    public const string SectionName = "CompanyPolicy";
    public string AllowedEmailDomain { get; init; } = "company.com";
    public string CompanyFoundedDate { get; init; } = "1999-01-01";
}