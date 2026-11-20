using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Notes;
using DomainCopilot.Domain.Workflows;
using DomainCopilot.Domain.Evidence;

namespace DomainCopilot.Application.Features.Agents;

public sealed class QueueClinicalDraftTool(
    AgentToolCatalog catalog,
    IRepository<ClinicalNoteDraft> drafts,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ClinicalNoteDraft> ExecuteAsync(
        string agentName,
        CopilotRun run,
        string content,
        IEnumerable<EvidenceCitation> citations,
        CancellationToken cancellationToken)
    {
        if (!catalog.CanInvoke(agentName, AgentToolNames.QueueDraftForReview))
            throw new AgentToolPermissionException(agentName, AgentToolNames.QueueDraftForReview);
        run.CompleteSafetyReview(true, timeProvider.GetUtcNow());
        var draft = new ClinicalNoteDraft(Guid.NewGuid(), run.Id, content, citations, timeProvider.GetUtcNow());
        run.SubmitForApproval(draft);
        drafts.Add(draft);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return draft;
    }
}
