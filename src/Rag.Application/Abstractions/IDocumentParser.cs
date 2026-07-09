namespace Rag.Application.Abstractions;

public interface IDocumentParser
{
    bool CanParse(string contentType, string fileName);
    Task<string> ParseAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken = default);
}
