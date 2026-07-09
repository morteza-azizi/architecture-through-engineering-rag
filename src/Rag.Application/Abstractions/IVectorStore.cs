using Rag.Domain.Documents;

namespace Rag.Application.Abstractions;

public interface IVectorStore
{
    Task IndexAsync(IReadOnlyList<DocumentChunk> chunks, IReadOnlyList<float[]> embeddings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
