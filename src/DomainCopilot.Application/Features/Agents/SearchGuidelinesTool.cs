using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed class SearchGuidelinesTool(AgentToolCatalog catalog, HybridSearchHandler search)
{
    public Task<SearchDocumentsResult> ExecuteAsync(
        string agentName,
        SearchDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        EnsurePermission(agentName);
        return search.HandleAsync(request, cancellationToken);
    }

    private void EnsurePermission(string agentName)
    {
        if (!catalog.CanInvoke(agentName, AgentToolNames.SearchGuidelines))
            throw new AgentToolPermissionException(agentName, AgentToolNames.SearchGuidelines);
    }
}
