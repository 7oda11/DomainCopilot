namespace DomainCopilot.Api.Contracts.Copilot;

public sealed class CreateDraftRequest
{
    public Guid CaseId { get; init; }
    public required string Question { get; init; }
    public int EvidenceLimit { get; init; } = 8;
}
