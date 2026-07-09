using Microsoft.AspNetCore.Mvc;

namespace Rag.Api.Contracts;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTimeOffset UploadedAt);

public sealed record ProblemResponse(
    string Title,
    string Detail,
    int Status);
