namespace GTSPolandHiring.WebApi.Infrastructure.Errors;
public class NotFoundError : BaseError
{
    public NotFoundError(string message = "The requested resource was not found.", string code = "RESOURCE_NOT_FOUND") 
        : base(code, message)
    {
        Metadata.Add("NotFound", true);
    }
}

public class ConflictError : BaseError
{
    public ConflictError(string message = "A data unicity conflict occurred.", string code = "RESOURCE_ALREADY_EXISTS") 
        : base(code, message)
    {
        Metadata.Add("Conflict", true);
    }
}

public class FileTooLargeError : BaseError
{
    public FileTooLargeError(long maxSizeBytes, string code = "FILE_TOO_LARGE") 
        : base(code, $"The uploaded file exceeds the maximum allowed size ({maxSizeBytes / 1024 / 1024} MB).")
    {
        Metadata.Add("ContentTooLarge", true);
    }
}

public class UnsupportedMediaTypeError : BaseError
{
    public UnsupportedMediaTypeError(string allowedFormats, string code = "UNSUPPORTED_MEDIA_TYPE") 
        : base(code, $"Unsupported media type. Allowed formats: {allowedFormats}.")
    {
        Metadata.Add("UnsupportedMediaType", true);
    }
}