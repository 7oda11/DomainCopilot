namespace DomainCopilot.Application.Abstractions.Documents;

public sealed record ExtractedPage(int PageNumber, string Text, string? Section = null);
