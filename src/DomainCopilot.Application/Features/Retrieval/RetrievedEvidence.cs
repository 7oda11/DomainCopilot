using DomainCopilot.Domain.Evidence;

namespace DomainCopilot.Application.Features.Retrieval;

public sealed record RetrievedEvidence(EvidenceCitation Citation, string SourceName, double Score);
