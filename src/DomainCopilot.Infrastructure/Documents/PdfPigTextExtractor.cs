using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;
using UglyToad.PdfPig;

namespace DomainCopilot.Infrastructure.Documents;

public sealed class PdfPigTextExtractor : IDocumentTextExtractor
{
    public bool Supports(DocumentFormat format) => format == DocumentFormat.Pdf;

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        DocumentFormat format,
        CancellationToken cancellationToken)
    {
        if (!Supports(format)) throw new NotSupportedException($"{format} is not supported.");
        using var document = PdfDocument.Open(content);
        var pages = new List<ExtractedPage>(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(new ExtractedPage(page.Number, page.Text));
        }

        return Task.FromResult<IReadOnlyList<ExtractedPage>>(pages);
    }
}
