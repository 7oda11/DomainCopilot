using DomainCopilot.Api.Contracts.Retrieval;
using DomainCopilot.Application.Features.Retrieval;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(HybridSearchHandler searchHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SearchDocumentsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchDocumentsResponse>> Search(
        [FromBody] DomainCopilot.Api.Contracts.Retrieval.SearchDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await searchHandler.HandleAsync(
                new DomainCopilot.Application.Features.Retrieval.SearchDocumentsRequest(request.Query, request.Limit), cancellationToken);
            var evidence = result.Evidence.Select(item => new SearchEvidenceResponse(
                item.Citation.DocumentId,
                item.Citation.ChunkId,
                item.SourceName,
                item.Citation.SourceVersion,
                item.Citation.Section,
                item.Citation.PageNumber,
                item.Citation.QuotedText,
                item.Score)).ToArray();
            return Ok(new SearchDocumentsResponse(result.Refused, result.RefusalReason, evidence));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid search request", Detail = exception.Message });
        }
    }
}
