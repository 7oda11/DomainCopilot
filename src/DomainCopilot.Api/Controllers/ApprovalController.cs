using System.Security.Claims;
using DomainCopilot.Api.Contracts.Approval;
using DomainCopilot.Application.Features.Approval;
using DomainCopilot.Domain.Approvals;
using DomainCopilot.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ClinicalStaff")]
[Route("api/drafts")]
public sealed class ApprovalController(ReviewClinicalDraftHandler reviewDraft) : ControllerBase
{
    [HttpPost("{draftId:guid}/review")]
    [ProducesResponseType<ReviewDraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDraftResponse>> Review(
        Guid draftId,
        [FromBody] ReviewDraftRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(reviewerId))
        {
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Authenticated staff member required." });
        }

        try
        {
            var result = await reviewDraft.HandleAsync(new ReviewClinicalDraftRequest(
                draftId, request.Action, request.Comment, request.ApprovedContent), reviewerId, cancellationToken);
            return Ok(new ReviewDraftResponse(result.RunId, result.DraftId, result.Action, result.Status, result.ApprovedContent));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Draft not found", Detail = exception.Message });
        }
        catch (DomainRuleViolationException exception)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = "Draft cannot be reviewed", Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid review request", Detail = exception.Message });
        }
    }
}
