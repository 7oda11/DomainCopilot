using System.IO.Compression;
using System.Text;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Documents;

namespace DomainCopilot.Infrastructure.Tests;

public sealed class OpenXmlDocxTextExtractorTests
{
    [Fact]
    public async Task Extracts_text_heading_and_explicit_page_breaks()
    {
        await using var package = CreateDocx("""
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
              <w:p><w:pPr><w:pStyle w:val="Heading1"/></w:pPr><w:r><w:t>Safety guideline</w:t></w:r></w:p>
              <w:p><w:r><w:t>First page content</w:t><w:br w:type="page"/></w:r></w:p>
              <w:p><w:r><w:t>Second page content</w:t></w:r></w:p>
            </w:body></w:document>
            """);

        var pages = await new OpenXmlDocxTextExtractor().ExtractAsync(package, DocumentFormat.Docx, CancellationToken.None);

        Assert.Equal(2, pages.Count);
        Assert.Equal(1, pages[0].PageNumber);
        Assert.Contains("First page content", pages[0].Text);
        Assert.Equal("Safety guideline", pages[0].Section);
        Assert.Equal(2, pages[1].PageNumber);
        Assert.Contains("Second page content", pages[1].Text);
    }

    private static MemoryStream CreateDocx(string documentXml)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        using (var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open(), Encoding.UTF8))
        {
            writer.Write(documentXml);
        }

        stream.Position = 0;
        return stream;
    }
}
