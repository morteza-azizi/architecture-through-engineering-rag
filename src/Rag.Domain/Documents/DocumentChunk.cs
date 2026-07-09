namespace Rag.Domain.Documents;

public sealed class DocumentChunk
{
    public required Guid Id { get; init; }
    public required Guid DocumentId { get; init; }
    public required int Index { get; init; }
    public required string Content { get; init; }
}
