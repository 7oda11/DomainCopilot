using DomainCopilot.Application.Abstractions.AI;
using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Application.Features.Agents;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Application.Features.Workflow;
using DomainCopilot.Application.Tests.Fakes;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Application.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomainCopilot.Application.Tests;

public sealed class RunCopilotWorkflowHandlerTests
{
    [Fact]
    public async Task Runs_all_three_agents_and_returns_only_a_clinician_review_draft()
    {
        var responses = new Queue<string>([
            "The source describes screening [E1].",
            "Draft note: screening is described [E1].",
            "{\"passed\":true,\"issues\":[]}" ]);
        var harness = CreateHandler(responses);

        var result = await harness.Handler.HandleAsync(new RunCopilotWorkflowRequest(harness.CaseId,
            "What does the guideline say about screening?"), CancellationToken.None);

        Assert.Equal("AwaitingApproval", result.Status);
        Assert.False(result.Refused);
        Assert.True(result.RequiresClinicianApproval);
        Assert.NotNull(result.DraftId);
        Assert.Equal(result.DraftId, harness.Runs.AddedRun?.DraftId);
        Assert.Equal(3, harness.UnitOfWork.SaveCount);
        Assert.Contains("[E1]", result.Draft);
        Assert.Equal(3, result.Trace.Count);
        Assert.All(result.Trace, item => Assert.Equal("completed", item.Outcome));
    }

    [Fact]
    public async Task Blocks_a_draft_when_safety_checker_rejects_it()
    {
        var responses = new Queue<string>([
            "The source describes screening [E1].",
            "Unsupported claim [E1].",
            "{\"passed\":false,\"issues\":[\"Claim is not supported.\"]}" ]);
        var harness = CreateHandler(responses);

        var result = await harness.Handler.HandleAsync(new RunCopilotWorkflowRequest(harness.CaseId, "Question"), CancellationToken.None);

        Assert.Equal("Refused", result.Status);
        Assert.True(result.Refused);
        Assert.Null(result.Draft);
        Assert.False(result.RequiresClinicianApproval);
    }

    [Fact]
    public async Task Returns_evidence_only_when_no_source_matches_without_calling_chat()
    {
        var provider = new FakeChatCompletionProvider("ollama", (_, _) => Task.FromResult(
            new ChatCompletionResponse("unused", "ollama", false, null, null)));
        var service = new FallbackChatCompletionService(provider, provider, NullLogger<FallbackChatCompletionService>.Instance);
        var harness = CreateHandler([], service, []);

        var result = await harness.Handler.HandleAsync(new RunCopilotWorkflowRequest(harness.CaseId, "Question"), CancellationToken.None);

        Assert.Equal("Refused", result.Status);
        Assert.Empty(result.Evidence);
        Assert.Equal(0, provider.CallCount);
    }

    private static (RunCopilotWorkflowHandler Handler, Guid CaseId, FakeCopilotRunRepository Runs, FakeUnitOfWork UnitOfWork) CreateHandler(
        Queue<string> responses,
        IChatCompletionService? service = null,
        IReadOnlyList<DocumentSearchCandidate>? candidates = null)
    {
        if (service is null)
        {
            var provider = new FakeChatCompletionProvider("ollama", (_, _) => Task.FromResult(
                new ChatCompletionResponse(responses.Dequeue(), "ollama", false, 20, 10)));
            service = new FallbackChatCompletionService(provider, provider, NullLogger<FallbackChatCompletionService>.Instance);
        }

        var documentCandidates = candidates ?? [new DocumentSearchCandidate(Guid.NewGuid(), Guid.NewGuid(),
            "Synthetic guideline", "v1", "The guideline describes screening at each visit.", "Screening", 2)];
        var vectorIndex = new FakeVectorIndex
        {
            SearchResults = documentCandidates.Select(item => new VectorMatch(item.ChunkId, 0.8)).ToArray()
        };
        var search = new HybridSearchHandler(new FakeEmbeddingGenerator(), vectorIndex,
            new FakeDocumentRetrievalStore(documentCandidates));
        var caseId = Guid.NewGuid();
        var clinicalCase = new ClinicalCase(caseId, "Synthetic case for unit tests.", TimeProvider.System.GetUtcNow());
        var runs = new FakeCopilotRunRepository();
        var unitOfWork = new FakeUnitOfWork();
        var catalog = new AgentToolCatalog();
        var handler = new RunCopilotWorkflowHandler(
            new SearchGuidelinesTool(catalog, search),
            new ReadClinicalCaseTool(catalog, new FakeClinicalCaseRepository(clinicalCase)),
            new ValidateCitationsTool(catalog),
            new QueueClinicalDraftTool(catalog, new FakeClinicalNoteDraftRepository(), unitOfWork, TimeProvider.System),
            new GuidelineResearcherAgent(service), new DocumentationDrafterAgent(service), new SafetyCheckerAgent(service),
            runs, unitOfWork, TimeProvider.System);
        return (handler, caseId, runs, unitOfWork);
    }
}
