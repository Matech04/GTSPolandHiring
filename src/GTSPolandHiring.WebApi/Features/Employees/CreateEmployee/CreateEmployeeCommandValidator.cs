using GTSPolandHiring.WebApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GTSPolandHiring.WebApi.Features.Employees.CreateEmployee;

public class CreateEmployeeCommandValidator : EmployeeInputValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator(IOptions<CompanyPolicyOptions> options) : base(options)
    {
    }
}
