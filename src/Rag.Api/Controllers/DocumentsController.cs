using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rag.Application.Abstractions;
using Rag.Application.Documents;
using Rag.Api.Contracts;
using Rag.Api.Infrastructure;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly DocumentUploadService _uploadService;
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentUploadOptions _uploadOptions;

    public DocumentsController(
        DocumentUploadService uploadService,
        IDocumentRepository documentRepository,
        IOptions<DocumentUploadOptions> uploadOptions)
    {
        _uploadService = uploadService;
        _documentRepository = documentRepository;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// Upload a Markdown or plain-text document for indexing.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No file provided",
                Detail = "Request must include a non-empty file."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _uploadService.UploadAsync(
            stream,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            _uploadOptions.MaxFileSizeBytes,
            cancellationToken);

        var response = DocumentMapping.ToResponse(result);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Get document metadata by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        return Ok(DocumentMapping.ToResponse(document));
    }
}
