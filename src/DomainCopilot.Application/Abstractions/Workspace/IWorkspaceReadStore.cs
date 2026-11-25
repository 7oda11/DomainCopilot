namespace DomainCopilot.Application.Abstractions.Workspace;

public sealed record WorkspaceCase(Guid CaseId, string Summary, DateTimeOffset CreatedAtUtc);

public sealed record WorkspaceDraft(Guid CaseId, Guid RunId, Guid? DraftId, string CaseSummary,
    string Question, string? DraftContent, string Status, DateTimeOffset CreatedAtUtc,
    string? Decision, DateTimeOffset? DecidedAtUtc, string? ReviewerId, string? Comment, string? ApprovedContent);

public sealed record WorkspaceDocument(Guid DocumentId, string FileName, string Format, string Version,
    string Status, DateTimeOffset UploadedAtUtc);

public sealed record WorkspaceDocumentContent(Guid DocumentId, string FileName, string Format,
    string Version, string Text);

public interface IWorkspaceReadStore
{
    Task<IReadOnlyList<WorkspaceCase>> GetCasesAsync(string userId, bool includeAll, CancellationToken cancellationToken);
    Task<WorkspaceCase?> GetCaseAsync(Guid caseId, string userId, bool includeAll, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkspaceDraft>> GetDraftsAsync(string userId, bool includeAll, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkspaceDocument>> GetDocumentsAsync(CancellationToken cancellationToken);
    Task<WorkspaceDocumentContent?> GetDocumentContentAsync(Guid documentId, CancellationToken cancellationToken);
}
