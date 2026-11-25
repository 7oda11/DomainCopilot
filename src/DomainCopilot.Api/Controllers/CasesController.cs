using DomainCopilot.Api.Contracts.Cases;
using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Application.Security;
using DomainCopilot.Application.Abstractions.Workspace;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ResearcherOrClinician")]
[Route("api/cases")]
public sealed class CasesController(IRepository<ClinicalCase> cases, IUnitOfWork unitOfWork,
    TimeProvider timeProvider, PiiRedactor piiRedactor, IWorkspaceReadStore workspace) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ClinicalCaseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClinicalCaseResponse>> Create(
        [FromBody] CreateClinicalCaseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SyntheticCaseSummary) || request.SyntheticCaseSummary.Length > 8000)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid synthetic case summary", Detail = "Provide 1 to 8,000 characters." });
        }

        var safeSummary = piiRedactor.Redact(request.SyntheticCaseSummary);
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var clinicalCase = new ClinicalCase(Guid.NewGuid(), safeSummary, timeProvider.GetUtcNow(), userId);
        cases.Add(clinicalCase);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { caseId = clinicalCase.Id },
            new ClinicalCaseResponse(clinicalCase.Id, clinicalCase.CreatedAtUtc));
    }

    [HttpGet("{caseId:guid}")]
    public async Task<ActionResult<ClinicalCaseDetailsResponse>> GetById(Guid caseId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var clinicalCase = await workspace.GetCaseAsync(caseId, userId,
            User.IsInRole("Clinician"), cancellationToken);
        if (clinicalCase is null) return NotFound();
        return Ok(new ClinicalCaseDetailsResponse(clinicalCase.CaseId, clinicalCase.Summary, clinicalCase.CreatedAtUtc));
    }
}
