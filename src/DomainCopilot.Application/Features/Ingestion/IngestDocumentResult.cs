namespace DomainCopilot.Application.Features.Ingestion;

public sealed record IngestDocumentResult(
    Guid DocumentId,
    bool WasAlreadyIndexed,
    int ChunkCount);
