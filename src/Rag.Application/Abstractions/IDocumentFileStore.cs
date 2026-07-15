namespace Rag.Application.Abstractions;

public interface IDocumentFileStore
{
    Task StoreAsync(Guid documentId, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
