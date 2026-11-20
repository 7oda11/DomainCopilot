using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Application.Tests.Fakes;

public sealed class FakeDocumentTextExtractor(IReadOnlyList<ExtractedPage> pages) : IDocumentTextExtractor
{
    public int ExtractionCount { get; private set; }
    public bool Supports(DocumentFormat format) => format == DocumentFormat.Pdf;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        ExtractionCount++;
        return Task.FromResult(pages);
    }
}
