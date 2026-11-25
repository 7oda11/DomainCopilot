using DomainCopilot.Application.Abstractions.Workspace;
using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Documents;
using DomainCopilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Infrastructure.Workspace;

public sealed class EfWorkspaceReadStore(AppDbContext db) : IWorkspaceReadStore
{
    public async Task<IReadOnlyList<WorkspaceCase>> GetCasesAsync(string userId, bool includeAll, CancellationToken cancellationToken) =>
        await db.ClinicalCases.AsNoTracking()
            .Where(item => includeAll || item.CreatedByUserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new WorkspaceCase(item.Id, item.CaseSummary, item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task<WorkspaceCase?> GetCaseAsync(Guid caseId, string userId, bool includeAll, CancellationToken cancellationToken) =>
        await db.ClinicalCases.AsNoTracking()
            .Where(item => item.Id == caseId && (includeAll || item.CreatedByUserId == userId))
            .Select(item => new WorkspaceCase(item.Id, item.CaseSummary, item.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspaceDraft>> GetDraftsAsync(string userId, bool includeAll, CancellationToken cancellationToken)
    {
        var rows = await (from run in db.CopilotRuns.AsNoTracking()
                          join clinicalCase in db.ClinicalCases.AsNoTracking() on run.CaseId equals clinicalCase.Id
                          join draftItem in db.ClinicalNoteDrafts.AsNoTracking() on run.Id equals draftItem.RunId into draftJoin
                          from draft in draftJoin.DefaultIfEmpty()
                          join approvalItem in db.ApprovalRecords.AsNoTracking() on draft.Id equals approvalItem.DraftId into approvalJoin
                          from approval in approvalJoin.DefaultIfEmpty()
                          where includeAll || run.CreatedByUserId == userId
                          orderby run.CreatedAtUtc descending
                          select new
                          {
                              run.CaseId, RunId = run.Id, DraftId = (Guid?)draft.Id,
                              clinicalCase.CaseSummary, run.Question, DraftContent = draft.Content,
                              run.Status, run.CreatedAtUtc, Decision = (ApprovalAction?)approval.Action,
                              DecidedAtUtc = (DateTimeOffset?)approval.DecidedAtUtc,
                              approval.ReviewerId, approval.Comment, approval.ApprovedContent
                          }).ToArrayAsync(cancellationToken);

        return rows.Select(row => new WorkspaceDraft(row.CaseId, row.RunId, row.DraftId, row.CaseSummary,
            row.Question, row.DraftContent, row.Status.ToString(), row.CreatedAtUtc, row.Decision?.ToString(),
            row.DecidedAtUtc, row.ReviewerId, row.Comment, row.ApprovedContent)).ToArray();
    }

    public async Task<IReadOnlyList<WorkspaceDocument>> GetDocumentsAsync(CancellationToken cancellationToken) =>
        await db.SourceDocuments.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new WorkspaceDocument(item.Id, item.SourceName, item.Format.ToString(), item.Version,
                item.Status.ToString(), item.CreatedAtUtc)).ToArrayAsync(cancellationToken);

    public async Task<WorkspaceDocumentContent?> GetDocumentContentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.SourceDocuments.AsNoTracking()
            .Where(item => item.Id == documentId)
            .Select(item => new { item.Id, item.SourceName, Format = item.Format.ToString(), item.Version })
            .SingleOrDefaultAsync(cancellationToken);
        if (document is null) return null;

        var chunks = await db.DocumentChunks.AsNoTracking().Where(item => item.DocumentId == documentId)
            .OrderBy(item => item.Ordinal).Select(item => item.Text).ToArrayAsync(cancellationToken);
        return new WorkspaceDocumentContent(document.Id, document.SourceName, document.Format, document.Version,
            string.Join(Environment.NewLine + Environment.NewLine, chunks));
    }
}
