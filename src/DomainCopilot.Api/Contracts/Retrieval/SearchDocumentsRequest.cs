namespace DomainCopilot.Api.Contracts.Retrieval;

public sealed class SearchDocumentsRequest
{
    public required string Query { get; init; }
    public int Limit { get; init; } = 5;
}
