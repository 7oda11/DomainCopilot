using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Abstractions.Documents;

public interface IDocumentTextExtractor
{
    bool Supports(DocumentFormat format);

    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        DocumentFormat format,
        CancellationToken cancellationToken);
}
