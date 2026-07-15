namespace Rag.Application.Documents;

public static class DocumentFormatPolicy
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md" };

    public static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName)
        && !Path.IsPathRooted(fileName)
        && fileName.IndexOfAny(['/', '\\', '\0']) < 0
        && fileName is not "." and not ".."
        && string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal);

    public static bool IsSupported(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName));
}
