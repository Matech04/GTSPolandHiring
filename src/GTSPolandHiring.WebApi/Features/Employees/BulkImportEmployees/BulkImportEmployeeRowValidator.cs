using GTSPolandHiring.WebApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Features.Employees.BulkImportEmployees;

public class BulkImportEmployeeRowValidator : EmployeeInputValidator<BulkImportEmployeeRow>
{
    public BulkImportEmployeeRowValidator(IOptions<CompanyPolicyOptions> options) : base(options)
    {
    }
}
