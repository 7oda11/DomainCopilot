using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed record ResearchRequest(string Question, IReadOnlyList<RetrievedEvidence> Evidence);
