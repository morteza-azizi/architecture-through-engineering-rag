namespace Rag.Application.Abstractions;

public interface ITextChunker
{
    IReadOnlyList<string> Chunk(string text);
}
