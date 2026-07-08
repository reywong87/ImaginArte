namespace ImaginArte.Domain;

public sealed class DomainRuleViolationException : InvalidOperationException
{
    public DomainRuleViolationException(DomainErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainErrorCode Code { get; }
}
