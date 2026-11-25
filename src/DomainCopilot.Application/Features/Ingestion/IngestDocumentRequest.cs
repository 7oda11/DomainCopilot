using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Features.Ingestion;

public sealed record IngestDocumentRequest(
    string SourceName,
    string Version,
    DocumentFormat Format,
    byte[] Content,
    string UploadedByUserId = "system");
