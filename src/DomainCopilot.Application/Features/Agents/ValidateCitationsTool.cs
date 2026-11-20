using System.Text.RegularExpressions;
using DomainCopilot.Application.Features.Retrieval;

namespace DomainCopilot.Application.Features.Agents;

public sealed partial class ValidateCitationsTool(AgentToolCatalog catalog)
{
    public bool Execute(string agentName, string draft, IReadOnlyList<RetrievedEvidence> evidence)
    {
        if (!catalog.CanInvoke(agentName, AgentToolNames.ValidateCitations))
            throw new AgentToolPermissionException(agentName, AgentToolNames.ValidateCitations);
        if (string.IsNullOrWhiteSpace(draft) || evidence.Count == 0) return false;
        var labels = CitationLabelPattern().Matches(draft).Select(match => int.Parse(match.Groups[1].Value)).ToArray();
        return labels.Length > 0 && labels.All(index => index >= 1 && index <= evidence.Count);
    }

    [GeneratedRegex("\\[E([1-9][0-9]*)\\]", RegexOptions.CultureInvariant)]
    private static partial Regex CitationLabelPattern();
}
