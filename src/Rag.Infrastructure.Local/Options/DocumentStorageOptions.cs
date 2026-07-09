namespace Rag.Infrastructure.Local.Options;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string BasePath { get; set; } = "./data";

    public string DatabasePath { get; set; } = "./data/rag.db";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
