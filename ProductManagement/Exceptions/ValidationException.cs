namespace ProductManagement.Exceptions;

public class ValidationException : BaseException
{
    public List<string> Errors { get; } 
    
    protected ValidationException(IEnumerable<string> errors) : 
        base("Validation failed", 400, "VALIDATION_ERROR")
    {
        Errors = errors.ToList();
    }

    protected ValidationException(string error) : base("Validation failed", 400, "VALIDATION_ERROR")
    {
        Errors = [error];
    }
}