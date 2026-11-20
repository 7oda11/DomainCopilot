using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Cases;

namespace DomainCopilot.Application.Features.Agents;

public sealed class ReadClinicalCaseTool(AgentToolCatalog catalog, IRepository<ClinicalCase> cases)
{
    public async Task<ClinicalCase?> ExecuteAsync(string agentName, Guid caseId, CancellationToken cancellationToken)
    {
        if (!catalog.CanInvoke(agentName, AgentToolNames.ReadClinicalCase))
            throw new AgentToolPermissionException(agentName, AgentToolNames.ReadClinicalCase);
        return await cases.GetByIdAsync(caseId, cancellationToken);
    }
}
