using Microsoft.Extensions.Logging;
using Rag.Application.Abstractions;
using Rag.Application.Exceptions;
using Rag.Domain.Documents;

namespace Rag.Application.Documents;

public sealed class DocumentUploadService(
    IDocumentRepository documentRepository,
    IDocumentFileStore documentFileStore,
    ILogger<DocumentUploadService> logger)
{
    public async Task<DocumentUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        long maxFileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (sizeBytes == 0)
        {
            throw new EmptyDocumentException();
        }

        if (sizeBytes > maxFileSizeBytes)
        {
            throw new DocumentTooLargeException(fileName, sizeBytes, maxFileSizeBytes);
        }

        if (!DocumentFormatPolicy.IsSupported(fileName))
        {
            throw new UnsupportedDocumentFormatException(fileName);
        }

        var documentId = Guid.NewGuid();
        var uploadedAt = DateTimeOffset.UtcNow;

        var document = new Document
        {
            Id = documentId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedAt = uploadedAt,
            Status = DocumentStatus.Pending
        };

        logger.LogInformation(
            "Uploading document {DocumentId} ({FileName}, {SizeBytes} bytes)",
            documentId,
            fileName,
            sizeBytes);

        await documentFileStore.StoreAsync(documentId, fileName, content, cancellationToken);
        await documentRepository.AddAsync(document, cancellationToken);

        logger.LogInformation("Document {DocumentId} stored successfully", documentId);

        return new DocumentUploadResult(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.Status,
            document.UploadedAt);
    }
}
