namespace DomainCopilot.Api.Contracts.Documents;

public sealed class UploadDocumentRequest
{
    public required IFormFile File { get; init; }
    public required string Version { get; init; }
}
