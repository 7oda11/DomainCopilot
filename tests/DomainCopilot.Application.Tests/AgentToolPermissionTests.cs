using DomainCopilot.Application.Features.Agents;

namespace DomainCopilot.Application.Tests;

public sealed class AgentToolPermissionTests
{
    [Fact]
    public void Each_agent_has_a_narrow_allow_list_and_only_orchestrator_can_queue_a_draft()
    {
        var catalog = new AgentToolCatalog();

        Assert.Equal([AgentToolNames.SearchGuidelines], catalog.GetAllowedTools("GuidelineResearcher"));
        Assert.Equal([AgentToolNames.ValidateCitations], catalog.GetAllowedTools("SafetyChecker"));
        Assert.Equal([AgentToolNames.ValidateCitations], catalog.GetAllowedTools("DocumentationDrafter"));
        Assert.Equal([AgentToolNames.ReadClinicalCase, AgentToolNames.QueueDraftForReview],
            catalog.GetAllowedTools("Orchestrator"));
        Assert.False(catalog.CanInvoke("DocumentationDrafter", AgentToolNames.QueueDraftForReview));
        Assert.False(catalog.CanInvoke("SafetyChecker", AgentToolNames.ReadClinicalCase));
    }

    [Fact]
    public void Citation_validator_rejects_out_of_range_evidence_labels()
    {
        var validator = new ValidateCitationsTool(new AgentToolCatalog());

        Assert.True(validator.Execute("DocumentationDrafter", "Claim [E1].",
            [new DomainCopilot.Application.Features.Retrieval.RetrievedEvidence(
                new DomainCopilot.Domain.Evidence.EvidenceCitation(
                    new DomainCopilot.Domain.Documents.DocumentChunk(Guid.NewGuid(), Guid.NewGuid(), 0,
                        "Source text.", null, 1, "v1"), "Source text."), "source", 1)]));
        Assert.False(validator.Execute("DocumentationDrafter", "Claim [E2].", [
            new DomainCopilot.Application.Features.Retrieval.RetrievedEvidence(
                new DomainCopilot.Domain.Evidence.EvidenceCitation(
                    new DomainCopilot.Domain.Documents.DocumentChunk(Guid.NewGuid(), Guid.NewGuid(), 0,
                        "Source text.", null, 1, "v1"), "Source text."), "source", 1)]));
    }
}
