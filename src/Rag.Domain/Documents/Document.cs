namespace Rag.Domain.Documents;

public sealed class Document
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public DocumentStatus Status { get; init; } = DocumentStatus.Pending;
}
