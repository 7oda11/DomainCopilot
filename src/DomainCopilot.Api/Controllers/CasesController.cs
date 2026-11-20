using DomainCopilot.Api.Contracts.Cases;
using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Domain.Cases;
using DomainCopilot.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Roles = "Clinician")]
[Route("api/cases")]
public sealed class CasesController(IRepository<ClinicalCase> cases, IUnitOfWork unitOfWork,
    TimeProvider timeProvider, PiiRedactor piiRedactor) : ControllerBase
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
        var clinicalCase = new ClinicalCase(Guid.NewGuid(), safeSummary, timeProvider.GetUtcNow());
        cases.Add(clinicalCase);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { caseId = clinicalCase.Id },
            new ClinicalCaseResponse(clinicalCase.Id, clinicalCase.CreatedAtUtc));
    }

    [HttpGet("{caseId:guid}")]
    public async Task<ActionResult<ClinicalCaseDetailsResponse>> GetById(Guid caseId, CancellationToken cancellationToken)
    {
        var clinicalCase = await cases.GetByIdAsync(caseId, cancellationToken);
        if (clinicalCase is null) return NotFound();
        return Ok(new ClinicalCaseDetailsResponse(clinicalCase.Id, clinicalCase.CaseSummary, clinicalCase.CreatedAtUtc));
    }
}
