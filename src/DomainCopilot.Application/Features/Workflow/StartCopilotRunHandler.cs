using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Application.Errors;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Features.Workflow;

public sealed class StartCopilotRunHandler(
    IRepository<ClinicalCase> cases,
    IRepository<CopilotRun> runs,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<StartCopilotRunResult> HandleAsync(
        StartCopilotRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clinicalCase = await cases.GetByIdAsync(request.CaseId, cancellationToken);
        if (clinicalCase is null)
        {
            throw new CaseNotFoundException(request.CaseId);
        }

        var createdAtUtc = timeProvider.GetUtcNow();
        var run = new CopilotRun(Guid.NewGuid(), clinicalCase, createdAtUtc);

        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartCopilotRunResult(run.Id, run.CaseId, run.Status, run.CreatedAtUtc);
    }
}
