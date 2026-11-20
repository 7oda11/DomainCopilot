using DomainCopilot.Application.Errors;
using DomainCopilot.Application.Features.Workflow;
using DomainCopilot.Application.Tests.Fakes;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Domain.Workflows;

namespace DomainCopilot.Application.Tests;

public sealed class StartCopilotRunHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Starts_a_run_and_saves_it_once()
    {
        var clinicalCase = new ClinicalCase(Guid.NewGuid(), "Synthetic case summary", NowUtc);
        var cases = new FakeClinicalCaseRepository(clinicalCase);
        var runs = new FakeCopilotRunRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new StartCopilotRunHandler(cases, runs, unitOfWork, new FixedTimeProvider(NowUtc));

        var result = await handler.HandleAsync(new StartCopilotRunRequest(clinicalCase.Id), CancellationToken.None);

        Assert.Equal(clinicalCase.Id, result.CaseId);
        Assert.Equal(CopilotRunStatus.Created, result.Status);
        Assert.Equal(NowUtc, result.CreatedAtUtc);
        Assert.Equal(result.RunId, runs.AddedRun?.Id);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_case_does_not_create_or_save_a_run()
    {
        var runs = new FakeCopilotRunRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new StartCopilotRunHandler(
            new FakeClinicalCaseRepository(null), runs, unitOfWork, new FixedTimeProvider(NowUtc));

        var exception = await Assert.ThrowsAsync<CaseNotFoundException>(() =>
            handler.HandleAsync(new StartCopilotRunRequest(Guid.NewGuid()), CancellationToken.None));

        Assert.NotEqual(Guid.Empty, exception.CaseId);
        Assert.Null(runs.AddedRun);
        Assert.Equal(0, unitOfWork.SaveCount);
    }
}
