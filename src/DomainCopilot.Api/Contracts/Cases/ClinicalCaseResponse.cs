namespace DomainCopilot.Api.Contracts.Cases;

public sealed record ClinicalCaseResponse(Guid CaseId, DateTimeOffset CreatedAtUtc);
