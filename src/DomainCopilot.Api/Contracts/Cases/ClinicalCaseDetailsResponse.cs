namespace DomainCopilot.Api.Contracts.Cases;

public sealed record ClinicalCaseDetailsResponse(Guid CaseId, string SyntheticCaseSummary, DateTimeOffset CreatedAtUtc);
