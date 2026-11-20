using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Documents;

namespace DomainCopilot.Infrastructure.Tests;

public sealed class PdfCorpusTests
{
    [Fact]
    public async Task Synthetic_corpus_has_thirty_documents_and_one_hundred_fifty_extractable_pages()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var corpusDirectory = Path.Combine(repositoryRoot, "data", "synthetic", "corpus");
        var files = Directory.GetFiles(corpusDirectory, "*.pdf");
        Assert.Equal(30, files.Length);

        var extractor = new PdfPigTextExtractor();
        var totalPages = 0;
        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var pages = await extractor.ExtractAsync(stream, DocumentFormat.Pdf, CancellationToken.None);
            Assert.Equal(5, pages.Count);
            Assert.All(pages, page => Assert.False(string.IsNullOrWhiteSpace(page.Text)));
            totalPages += pages.Count;
        }

        Assert.Equal(150, totalPages);
    }
}
