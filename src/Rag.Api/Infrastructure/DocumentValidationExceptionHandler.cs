using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rag.Application.Exceptions;
using Rag.Application.Documents;
using Rag.Domain.Documents;
using Rag.Api.Contracts;

namespace Rag.Api.Infrastructure;

public sealed class DocumentValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DocumentValidationExceptionHandler> _logger;

    public DocumentValidationExceptionHandler(ILogger<DocumentValidationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DocumentValidationException validationException)
        {
            return false;
        }

        _logger.LogWarning(validationException, "Document validation failed");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Document validation failed",
            Detail = validationException.Message,
            Type = "https://httpstatuses.com/400"
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}

internal static class DocumentMapping
{
    public static DocumentResponse ToResponse(Document document) =>
        new(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.Status.ToString(),
            document.UploadedAt);

    public static DocumentResponse ToResponse(DocumentUploadResult result) =>
        new(
            result.Id,
            result.FileName,
            result.ContentType,
            result.SizeBytes,
            result.Status.ToString(),
            result.UploadedAt);
}
