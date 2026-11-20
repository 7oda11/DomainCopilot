namespace DomainCopilot.Application.Errors;

public sealed class CaseNotFoundException(Guid caseId)
    : Exception($"Clinical case '{caseId}' was not found.")
{
    public Guid CaseId { get; } = caseId;
}
