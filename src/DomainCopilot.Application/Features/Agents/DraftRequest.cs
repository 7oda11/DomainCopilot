using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed record DraftRequest(string Question, string ResearchFindings, IReadOnlyList<RetrievedEvidence> Evidence);
