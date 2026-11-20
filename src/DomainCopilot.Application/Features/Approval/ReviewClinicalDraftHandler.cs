using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Application.Features.Agents;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Errors;
using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Features.Approval;

public sealed class ReviewClinicalDraftHandler(
    IRepository<ClinicalNoteDraft> drafts,
    IRepository<CopilotRun> runs,
    IRepository<ApprovalRecord> approvals,
    IUnitOfWork unitOfWork,
    SafetyCheckerAgent safetyChecker,
    TimeProvider timeProvider)
{
    public async Task<ReviewClinicalDraftResult> HandleAsync(
        ReviewClinicalDraftRequest request,
        string reviewerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(reviewerId)) throw new ArgumentException("Reviewer identity is required.", nameof(reviewerId));
        var draft = await drafts.GetByIdAsync(request.DraftId, cancellationToken)
            ?? throw new KeyNotFoundException($"Clinical draft '{request.DraftId}' was not found.");
        var run = await runs.GetByIdAsync(draft.RunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Copilot run '{draft.RunId}' was not found.");
        if (run.Status != CopilotRunStatus.AwaitingApproval)
        {
            throw new DomainRuleViolationException("approval.run.not_pending", "This draft is no longer awaiting clinician approval.");
        }

        if (request.Action == ApprovalAction.EditedAndApproved)
        {
            var evidence = draft.Citations.Select(citation => new RetrievedEvidence(citation, "source", 1)).ToArray();
            var review = await safetyChecker.RunAsync(
                new SafetyReviewRequest("Review the clinician-edited note for evidence support.", request.ApprovedContent ?? string.Empty, evidence),
                cancellationToken);
            if (!review.Passed)
            {
                throw new DomainRuleViolationException("approval.edited_content.unsafe",
                    string.Join("; ", review.Issues));
            }
        }

        var decision = new ApprovalRecord(Guid.NewGuid(), draft.Id, reviewerId, request.Action,
            timeProvider.GetUtcNow(), request.Comment, request.ApprovedContent);
        if (request.Action == ApprovalAction.Rejected) run.Reject(decision);
        else run.Approve(decision);
        approvals.Add(decision);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReviewClinicalDraftResult(run.Id, draft.Id, decision.Action,
            run.Status.ToString(), decision.ApprovedContent ?? draft.Content);
    }
}
