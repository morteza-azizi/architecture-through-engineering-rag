using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rag.Application.Abstractions;
using Rag.Infrastructure.Local.Options;

namespace Rag.Infrastructure.Local.Storage;

public sealed class LocalDocumentFileStore : IDocumentFileStore
{
    private readonly string _basePath;
    private readonly ILogger<LocalDocumentFileStore> _logger;

    public LocalDocumentFileStore(
        IOptions<DocumentStorageOptions> options,
        ILogger<LocalDocumentFileStore> logger)
    {
        _basePath = Path.GetFullPath(options.Value.BasePath);
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task StoreAsync(
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var documentDirectory = GetDocumentDirectory(documentId);
        Directory.CreateDirectory(documentDirectory);

        var destinationPath = Path.Combine(documentDirectory, fileName);

        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogDebug(
            "Stored document file {DocumentId} at {Path}",
            documentId,
            destinationPath);
    }

    public Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var documentDirectory = GetDocumentDirectory(documentId);

        if (!Directory.Exists(documentDirectory))
        {
            throw new FileNotFoundException($"No stored files found for document {documentId}.");
        }

        var filePath = Directory.GetFiles(documentDirectory).SingleOrDefault()
            ?? throw new FileNotFoundException($"No stored files found for document {documentId}.");

        Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return Task.FromResult(stream);
    }

    private string GetDocumentDirectory(Guid documentId) =>
        Path.Combine(_basePath, "documents", documentId.ToString());
}
