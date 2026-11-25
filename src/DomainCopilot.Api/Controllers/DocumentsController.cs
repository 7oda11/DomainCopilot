using DomainCopilot.Api.Contracts.Documents;
using DomainCopilot.Application.Features.Ingestion;
using DomainCopilot.Domain.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DomainCopilot.Application.Abstractions.Workspace;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Authorize(Policy = "ResearcherOrClinician")]
[Route("api/documents")]
public sealed class DocumentsController(IngestDocumentHandler ingestDocument, IWorkspaceReadStore workspace) : ControllerBase
{
    private const long MaximumUploadBytes = 25 * 1024 * 1024;

    [Authorize(Policy = "Researcher")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumUploadBytes)]
    [ProducesResponseType<UploadDocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.File.FileName);
        var format = extension.ToLowerInvariant() switch
        {
            ".pdf" => DocumentFormat.Pdf,
            ".docx" => DocumentFormat.Docx,
            _ => (DocumentFormat?)null
        };
        if (format is null)
        {
            return Problem(statusCode: StatusCodes.Status415UnsupportedMediaType,
                title: "Unsupported document format", detail: "Upload a PDF or DOCX file.");
        }

        if (request.File.Length is <= 0 or > MaximumUploadBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid document size",
                Detail = "The file must be between 1 byte and 25 MiB."
            });
        }

        await using var buffer = new MemoryStream(checked((int)request.File.Length));
        await request.File.CopyToAsync(buffer, cancellationToken);
        IngestDocumentResult result;
        try
        {
            result = await ingestDocument.HandleAsync(
                new IngestDocumentRequest(Path.GetFileName(request.File.FileName), request.Version, format.Value,
                    buffer.ToArray(), User.FindFirstValue("sub") ?? string.Empty),
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid document request", Detail = exception.Message });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid document content", Detail = exception.Message });
        }

        return Ok(new UploadDocumentResponse(result.DocumentId, result.WasAlreadyIndexed, result.ChunkCount));
    }

    [Authorize(Policy = "Clinician")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkspaceDocument>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await workspace.GetDocumentsAsync(cancellationToken));

    [Authorize(Policy = "Clinician")]
    [HttpGet("{documentId:guid}/content")]
    public async Task<ActionResult<WorkspaceDocumentContent>> GetContent(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await workspace.GetDocumentContentAsync(documentId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }
}
