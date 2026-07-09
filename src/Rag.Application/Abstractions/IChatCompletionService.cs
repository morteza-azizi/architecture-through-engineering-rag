namespace Rag.Application.Abstractions;

public sealed record ChatMessage(string Role, string Content);

public interface IChatCompletionService
{
    Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
