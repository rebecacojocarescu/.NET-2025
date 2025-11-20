using Microsoft.AspNetCore.Http;

namespace OrderManagement.Exceptions;

public class ValidationException : BaseException
{
    public IReadOnlyCollection<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed", StatusCodes.Status400BadRequest, "VALIDATION_ERROR")
    {
        Errors = errors.ToArray();
    }

    public ValidationException(string error)
        : this(new[] { error })
    {
    }
}

