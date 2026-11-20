using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Features.Workflow;

public sealed record StartCopilotRunResult(
    Guid RunId,
    Guid CaseId,
    CopilotRunStatus Status,
    DateTimeOffset CreatedAtUtc);
