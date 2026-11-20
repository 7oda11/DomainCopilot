namespace DomainCopilot.Application.Features.Agents;

public sealed class AgentToolCatalog
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedTools =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["GuidelineResearcher"] = [AgentToolNames.SearchGuidelines],
            ["SafetyChecker"] = [AgentToolNames.ValidateCitations],
            ["DocumentationDrafter"] = [AgentToolNames.ValidateCitations],
            ["Orchestrator"] = [AgentToolNames.ReadClinicalCase, AgentToolNames.QueueDraftForReview]
        };

    public bool CanInvoke(string agentName, string toolName) =>
        AllowedTools.TryGetValue(agentName, out var tools) && tools.Contains(toolName);

    public IReadOnlyCollection<string> GetAllowedTools(string agentName) =>
        AllowedTools.TryGetValue(agentName, out var tools) ? tools.ToArray() : [];
}
