namespace Rag.Application.Documents;

public static class DocumentFormatPolicy
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md" };

    public static bool IsSupported(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName));
}
