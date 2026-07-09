using Rag.Domain.Documents;

namespace Rag.Application.Abstractions;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, DocumentStatus status, CancellationToken cancellationToken = default);
}
