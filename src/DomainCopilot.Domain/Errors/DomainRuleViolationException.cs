namespace DomainCopilot.Domain.Errors;

public sealed class DomainRuleViolationException(string code, string message) : Exception(message)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(code)
        ? throw new ArgumentException("An error code is required.", nameof(code))
        : code;
}
