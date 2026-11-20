namespace DomainCopilot.Api.Contracts.Documents;

public sealed record UploadDocumentResponse(Guid DocumentId, bool AlreadyIndexed, int ChunkCount);
