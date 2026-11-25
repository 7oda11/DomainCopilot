using System.Security.Claims;
using DomainCopilot.Application.Abstractions.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ResearcherOrClinician")]
[Route("api/workspace")]
public sealed class WorkspaceController(IWorkspaceReadStore workspace) : ControllerBase
{
    [HttpGet("cases")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceCase>>> GetCases(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return Ok(await workspace.GetCasesAsync(userId, User.IsInRole("Clinician"), cancellationToken));
    }

    [HttpGet("drafts")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceDraft>>> GetDrafts(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return Ok(await workspace.GetDraftsAsync(userId, User.IsInRole("Clinician"), cancellationToken));
    }
}
