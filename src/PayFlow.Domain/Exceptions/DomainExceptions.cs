namespace PayFlow.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class PayrollValidationException : DomainException
{
    public List<string> Errors { get; } = new();

    public PayrollValidationException(string message) : base(message)
    {
        Errors.Add(message);
    }

    public PayrollValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors.AddRange(errors);
    }
}

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key) 
        : base($"Entity '{entityName}' with identifier '{key}' was not found.") { }
}

public class UnauthorizedDomainAccessException : DomainException
{
    public UnauthorizedDomainAccessException(string message) : base(message) { }
}
