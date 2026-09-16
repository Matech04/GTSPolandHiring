using GTSPolandHiring.WebApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Features.Employees.UpdateEmployee;

public class UpdateEmployeeCommandValidator : EmployeeInputValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator(IOptions<CompanyPolicyOptions> options) : base(options)
    {
    }
}
