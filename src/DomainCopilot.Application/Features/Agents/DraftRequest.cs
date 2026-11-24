using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed record DraftRequest(string Question, IReadOnlyList<RetrievedEvidence> Evidence);
