using Rag.Domain.Documents;

namespace Rag.Application.Documents;

public sealed record DocumentUploadResult(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentStatus Status,
    DateTimeOffset UploadedAt);
