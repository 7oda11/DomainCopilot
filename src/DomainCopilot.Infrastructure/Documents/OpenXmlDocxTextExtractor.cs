using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Domain.Documents;

namespace DomainCopilot.Infrastructure.Documents;

public sealed class OpenXmlDocxTextExtractor : IDocumentTextExtractor
{
    private static readonly XNamespace Word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XName TextElement = Word + "t";

    public bool Supports(DocumentFormat format) => format == DocumentFormat.Docx;

    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        DocumentFormat format,
        CancellationToken cancellationToken)
    {
        if (!Supports(format)) throw new NotSupportedException($"{format} is not supported.");
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("The DOCX package does not contain word/document.xml.");
        if (entry.Length > 20 * 1024 * 1024)
        {
            throw new InvalidDataException("The uncompressed DOCX document body exceeds 20 MiB.");
        }

        await using var xmlStream = entry.Open();
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            MaxCharactersInDocument = 5_000_000,
            XmlResolver = null
        });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        var body = document.Root?.Element(Word + "body")
            ?? throw new InvalidDataException("The DOCX document body is missing.");

        var pages = new List<ExtractedPage>();
        var pageNumber = 1;
        var section = (string?)null;
        var pageText = new List<string>();
        foreach (var paragraph in body.Elements(Word + "p"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var style = (string?)paragraph.Element(Word + "pPr")?.Element(Word + "pStyle")?.Attribute(Word + "val");
            var text = string.Concat(paragraph.Descendants(TextElement).Select(element => element.Value));
            var beginsNewPage = paragraph.Element(Word + "pPr")?.Element(Word + "pageBreakBefore") is not null;
            if (beginsNewPage && pageText.Count > 0)
            {
                pages.Add(new ExtractedPage(pageNumber++, string.Join(" ", pageText), section));
                pageText.Clear();
            }

            if (string.IsNullOrWhiteSpace(text)) continue;
            if (style?.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) == true)
            {
                section = text.Trim();
            }

            var hasRenderedPageBreak = paragraph.Descendants(Word + "lastRenderedPageBreak").Any();
            var hasExplicitPageBreak = paragraph.Descendants(Word + "br")
                .Any(element => (string?)element.Attribute(Word + "type") == "page");
            pageText.Add(text.Trim());
            if (hasRenderedPageBreak || hasExplicitPageBreak)
            {
                pages.Add(new ExtractedPage(pageNumber++, string.Join(" ", pageText), section));
                pageText.Clear();
            }
        }

        if (pageText.Count > 0 || pages.Count == 0)
        {
            pages.Add(new ExtractedPage(pageNumber, string.Join(" ", pageText), section));
        }

        return pages;
    }
}
