using DomainCopilot.Api.Contracts.Copilot;
using DomainCopilot.Api.Contracts.Retrieval;
using DomainCopilot.Application.Features.Workflow;
using DomainCopilot.Application.Abstractions.Observability;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ResearcherOrClinician")]
[Route("api/copilot/drafts")]
public sealed class CopilotController(RunCopilotWorkflowHandler workflow, IWorkflowTraceStore traceStore) : ControllerBase
{
    [HttpPost("stream")]
    [Produces("text/event-stream")]
    public async Task StreamDraft(
        [FromBody] CreateDraftRequest request,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        var channel = Channel.CreateBounded<WorkflowTraceResponse>(new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        var workflowTask = Task.Run(async () =>
        {
            try
            {
                return await workflow.HandleAsync(
                    new RunCopilotWorkflowRequest(request.CaseId, request.Question, request.EvidenceLimit,
                        User.FindFirstValue("sub") ?? string.Empty, User.IsInRole("Clinician")),
                    cancellationToken,
                    trace => channel.Writer.TryWrite(new WorkflowTraceResponse(
                        trace.AgentName, trace.Outcome, trace.ProviderId, trace.IsDegraded, trace.ElapsedMilliseconds)));
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, CancellationToken.None);

        await foreach (var progress in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await WriteEventAsync("progress", progress, cancellationToken);
        }

        try
        {
            var result = await workflowTask;
            var response = new CreateDraftResponse(result.RunId, result.DraftId, result.Status, result.Draft,
                result.Refused, result.RefusalReason, result.RequiresClinicianApproval, result.ActiveProvider,
                result.IsDegraded, result.Evidence.Select(item => new SearchEvidenceResponse(
                    item.Citation.DocumentId, item.Citation.ChunkId, item.SourceName, item.Citation.SourceVersion,
                    item.Citation.Section, item.Citation.PageNumber, item.Citation.QuotedText, item.Score)).ToArray(),
                result.Trace.Select(item => new WorkflowTraceResponse(
                    item.AgentName, item.Outcome, item.ProviderId, item.IsDegraded, item.ElapsedMilliseconds,
                    item.InputTokens, item.OutputTokens, item.EstimatedCostUsd)).ToArray());
            await WriteEventAsync("completed", response, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            await WriteEventAsync("error", new ProblemDetails { Status = 400, Title = "Invalid copilot request", Detail = exception.Message }, cancellationToken);
        }
    }

    [HttpPost]
    [ProducesResponseType<CreateDraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateDraftResponse>> CreateDraft(
        [FromBody] CreateDraftRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await workflow.HandleAsync(
                new RunCopilotWorkflowRequest(request.CaseId, request.Question, request.EvidenceLimit,
                    User.FindFirstValue("sub") ?? string.Empty, User.IsInRole("Clinician")), cancellationToken);
            var evidence = result.Evidence.Select(item => new SearchEvidenceResponse(
                item.Citation.DocumentId, item.Citation.ChunkId, item.SourceName, item.Citation.SourceVersion,
                item.Citation.Section, item.Citation.PageNumber, item.Citation.QuotedText, item.Score)).ToArray();
            var trace = result.Trace.Select(item => new WorkflowTraceResponse(
                item.AgentName, item.Outcome, item.ProviderId, item.IsDegraded, item.ElapsedMilliseconds,
                item.InputTokens, item.OutputTokens, item.EstimatedCostUsd)).ToArray();
            return Ok(new CreateDraftResponse(result.RunId, result.DraftId, result.Status, result.Draft, result.Refused,
                result.RefusalReason, result.RequiresClinicianApproval, result.ActiveProvider,
                result.IsDegraded, evidence, trace));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid copilot request", Detail = exception.Message });
        }
    }

    [HttpGet("/api/copilot/runs/{runId:guid}/trace")]
    [ProducesResponseType<WorkflowTraceResponse[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkflowTraceResponse>>> GetTrace(Guid runId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var traces = await traceStore.GetAsync(runId, userId, User.IsInRole("Clinician"), cancellationToken);
        if (traces.Count == 0) return NotFound(new ProblemDetails { Status = 404, Title = "Run trace not found" });
        return Ok(traces.Select(item => new WorkflowTraceResponse(item.AgentName, item.Outcome, item.ProviderId,
            item.IsDegraded, item.ElapsedMilliseconds, item.InputTokens, item.OutputTokens, item.EstimatedCostUsd)).ToArray());
    }

    private async Task WriteEventAsync<T>(string eventName, T payload, CancellationToken cancellationToken)
    {
        // This response is written directly to the SSE stream, so MVC's configured
        // JSON naming policy does not apply. Use the same camelCase shape as the
        // regular API responses consumed by the Angular client.
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
