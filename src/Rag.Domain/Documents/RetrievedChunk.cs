namespace Rag.Domain.Documents;

public sealed class RetrievedChunk
{
    public required DocumentChunk Chunk { get; init; }
    public required float Score { get; init; }
}
