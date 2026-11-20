namespace DomainCopilot.Application.Features.Ingestion;

public sealed record ChunkText(string Text, int PageNumber, string? Section);
