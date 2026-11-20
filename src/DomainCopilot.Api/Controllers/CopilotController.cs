using DomainCopilot.Api.Contracts.Copilot;
using DomainCopilot.Api.Contracts.Retrieval;
using DomainCopilot.Application.Features.Workflow;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ClinicalStaff")]
[Route("api/copilot/drafts")]
public sealed class CopilotController(RunCopilotWorkflowHandler workflow) : ControllerBase
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
                    new RunCopilotWorkflowRequest(request.CaseId, request.Question, request.EvidenceLimit),
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
                    item.AgentName, item.Outcome, item.ProviderId, item.IsDegraded, item.ElapsedMilliseconds)).ToArray());
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
                new RunCopilotWorkflowRequest(request.CaseId, request.Question, request.EvidenceLimit), cancellationToken);
            var evidence = result.Evidence.Select(item => new SearchEvidenceResponse(
                item.Citation.DocumentId, item.Citation.ChunkId, item.SourceName, item.Citation.SourceVersion,
                item.Citation.Section, item.Citation.PageNumber, item.Citation.QuotedText, item.Score)).ToArray();
            var trace = result.Trace.Select(item => new WorkflowTraceResponse(
                item.AgentName, item.Outcome, item.ProviderId, item.IsDegraded, item.ElapsedMilliseconds)).ToArray();
            return Ok(new CreateDraftResponse(result.RunId, result.DraftId, result.Status, result.Draft, result.Refused,
                result.RefusalReason, result.RequiresClinicianApproval, result.ActiveProvider,
                result.IsDegraded, evidence, trace));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid copilot request", Detail = exception.Message });
        }
    }

    private async Task WriteEventAsync<T>(string eventName, T payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
