using GTSPolandHiring.WebApi.Infrastructure.Errors;
using FluentResults;
using FluentValidation;
using MediatR;

namespace GTSPolandHiring.WebApi.Infrastructure.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var response = new TResponse();
            foreach (var failure in failures)
            {
                var error = new Error(failure.ErrorMessage);
                var errorCode = !string.IsNullOrEmpty(failure.ErrorCode) 
                    ? failure.ErrorCode 
                    : "VALIDATION_ERROR";
                
                error.Metadata.Add("ErrorCode", errorCode);
                response.Reasons.Add(error);
            }

            return response;
        }

        return await next();
    }
}