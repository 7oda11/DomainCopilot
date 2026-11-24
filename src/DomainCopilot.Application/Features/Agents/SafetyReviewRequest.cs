using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed record SafetyReviewRequest(
    string Question,
    string Draft,
    IReadOnlyList<RetrievedEvidence> Evidence,
    string? ResearchFindings = null);
