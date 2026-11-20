namespace DomainCopilot.Api.Contracts.Retrieval;

public sealed record SearchDocumentsResponse(
    bool Refused,
    string? RefusalReason,
    IReadOnlyList<SearchEvidenceResponse> Evidence);
