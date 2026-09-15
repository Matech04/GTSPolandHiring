using FluentResults;

namespace GTSPolandHiring.WebApi.Infrastructure.Errors;

public abstract class BaseError : Error
{
    public string Code { get; }

    protected BaseError(string code, string? customMessage = null) 
        : base(customMessage ?? code)
    {
        Code = code;
        Metadata.Add("ErrorCode", code);
    }
}