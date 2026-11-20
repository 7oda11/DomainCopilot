using DomainCopilot.Application.Abstractions.Documents;

namespace DomainCopilot.Application.Features.Ingestion;

public sealed class DocumentChunker
{
    private readonly int _maxCharacters;
    private readonly int _overlapCharacters;

    public DocumentChunker(int maxCharacters = 1200, int overlapCharacters = 160)
    {
        if (maxCharacters < 100 || overlapCharacters < 0 || overlapCharacters >= maxCharacters)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharacters), "Chunk size must be at least 100 and larger than a non-negative overlap.");
        }

        _maxCharacters = maxCharacters;
        _overlapCharacters = overlapCharacters;
    }

    public IReadOnlyList<ChunkText> Chunk(IReadOnlyList<ExtractedPage> pages)
    {
        var result = new List<ChunkText>();
        foreach (var page in pages)
        {
            var text = string.Join(' ', page.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var offset = 0;
            while (offset < text.Length)
            {
                var end = Math.Min(offset + _maxCharacters, text.Length);
                if (end < text.Length)
                {
                    var boundary = text.LastIndexOfAny(['.', '!', '?', ';', ' '], end - 1, end - offset);
                    if (boundary > offset + _maxCharacters / 2)
                    {
                        end = boundary + 1;
                    }
                }

                var chunk = text[offset..end].Trim();
                if (chunk.Length > 0)
                {
                    result.Add(new ChunkText(chunk, page.PageNumber, page.Section));
                }

                if (end >= text.Length)
                {
                    break;
                }

                offset = Math.Max(end - _overlapCharacters, offset + 1);
            }
        }

        return result;
    }
}
