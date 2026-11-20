namespace DomainCopilot.Application.Features.Retrieval;

public sealed record SearchDocumentsRequest(string Query, int Limit = 5);
