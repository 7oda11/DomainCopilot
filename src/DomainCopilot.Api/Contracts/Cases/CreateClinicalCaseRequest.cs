namespace DomainCopilot.Api.Contracts.Cases;

public sealed class CreateClinicalCaseRequest
{
    public required string SyntheticCaseSummary { get; init; }
}
