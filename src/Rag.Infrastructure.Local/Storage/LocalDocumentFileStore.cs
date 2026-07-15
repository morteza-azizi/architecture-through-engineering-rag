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
        var destinationPath = GetSafeDestinationPath(documentDirectory, fileName);
        Directory.CreateDirectory(documentDirectory);

        try
        {
            await using var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

            await content.CopyToAsync(fileStream, cancellationToken);
        }
        catch
        {
            try
            {
                File.Delete(destinationPath);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to remove partial file for document {DocumentId}",
                    documentId);
            }

            throw;
        }

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

    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documentDirectory = GetDocumentDirectory(documentId);
        if (Directory.Exists(documentDirectory))
        {
            Directory.Delete(documentDirectory, recursive: true);
            _logger.LogDebug("Deleted stored files for document {DocumentId}", documentId);
        }

        return Task.CompletedTask;
    }

    private string GetDocumentDirectory(Guid documentId) =>
        Path.Combine(_basePath, "documents", documentId.ToString());

    private static string GetSafeDestinationPath(string documentDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || Path.IsPathRooted(fileName)
            || fileName.IndexOfAny(['/', '\\', '\0']) >= 0
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            throw new ArgumentException("Directory paths are not allowed in document file names.", nameof(fileName));
        }

        var fullDirectoryPath = Path.GetFullPath(documentDirectory);
        var destinationPath = Path.GetFullPath(Path.Combine(fullDirectoryPath, fileName));
        var relativePath = Path.GetRelativePath(fullDirectoryPath, destinationPath);

        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new ArgumentException("Document file path must remain inside its storage directory.", nameof(fileName));
        }

        return destinationPath;
    }
}
