using FluentAssertions;
using Rag.Application.Documents;
using Rag.Application.Exceptions;

namespace Rag.Application.Tests;

public sealed class DocumentFormatPolicyTests
{
    [Theory]
    [InlineData("notes.txt")]
    [InlineData("README.md")]
    [InlineData("guide.MD")]
    public void IsSupported_AllowsTextAndMarkdown(string fileName)
    {
        DocumentFormatPolicy.IsSupported(fileName).Should().BeTrue();
    }

    [Theory]
    [InlineData("notes.pdf")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    public void IsSupported_RejectsOtherFormats(string fileName)
    {
        DocumentFormatPolicy.IsSupported(fileName).Should().BeFalse();
    }
}

public sealed class DocumentUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_UnsupportedFormat_Throws()
    {
        var service = new DocumentUploadService(
            new FakeDocumentRepository(),
            new FakeDocumentFileStore(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentUploadService>.Instance);

        var act = () => service.UploadAsync(
            new MemoryStream("content"u8.ToArray()),
            "report.pdf",
            "application/pdf",
            7,
            1024,
            CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedDocumentFormatException>();
    }
}

file sealed class FakeDocumentRepository : Rag.Application.Abstractions.IDocumentRepository
{
    public Task<Rag.Domain.Documents.Document> AddAsync(
        Rag.Domain.Documents.Document document,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(document);

    public Task<Rag.Domain.Documents.Document?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Rag.Domain.Documents.Document?>(null);

    public Task UpdateStatusAsync(
        Guid id,
        Rag.Domain.Documents.DocumentStatus status,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class FakeDocumentFileStore : Rag.Application.Abstractions.IDocumentFileStore
{
    public Task StoreAsync(
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream());
}
