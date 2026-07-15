namespace Rag.Application.Documents;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "DocumentUpload";
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
}
