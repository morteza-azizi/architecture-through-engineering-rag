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

    [Theory]
    [InlineData("../notes.md")]
    [InlineData("folder/notes.md")]
    [InlineData("folder\\notes.md")]
    [InlineData("/tmp/notes.md")]
    public void IsSafeFileName_RejectsPaths(string fileName)
    {
        DocumentFormatPolicy.IsSafeFileName(fileName).Should().BeFalse();
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("architecture guide.md")]
    public void IsSafeFileName_AllowsFileNamesWithoutPaths(string fileName)
    {
        DocumentFormatPolicy.IsSafeFileName(fileName).Should().BeTrue();
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

    [Fact]
    public async Task UploadAsync_EmptyDocument_ThrowsBeforeWriting()
    {
        var repository = new FakeDocumentRepository();
        var fileStore = new FakeDocumentFileStore();
        var service = CreateService(repository, fileStore);

        var act = () => service.UploadAsync(
            new MemoryStream(),
            "empty.txt",
            "text/plain",
            0,
            1024,
            CancellationToken.None);

        await act.Should().ThrowAsync<EmptyDocumentException>();
        repository.AddCalled.Should().BeFalse();
        fileStore.StoreCalled.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_DocumentExceedsLimit_ThrowsBeforeWriting()
    {
        var repository = new FakeDocumentRepository();
        var fileStore = new FakeDocumentFileStore();
        var service = CreateService(repository, fileStore);

        var act = () => service.UploadAsync(
            new MemoryStream("content"u8.ToArray()),
            "large.txt",
            "text/plain",
            1025,
            1024,
            CancellationToken.None);

        await act.Should().ThrowAsync<DocumentTooLargeException>();
        repository.AddCalled.Should().BeFalse();
        fileStore.StoreCalled.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_UnsafeFileName_ThrowsBeforeWriting()
    {
        var repository = new FakeDocumentRepository();
        var fileStore = new FakeDocumentFileStore();
        var service = CreateService(repository, fileStore);

        var act = () => service.UploadAsync(
            new MemoryStream("content"u8.ToArray()),
            "../report.md",
            "text/markdown",
            7,
            1024,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDocumentFileNameException>();
        repository.AddCalled.Should().BeFalse();
        fileStore.StoreCalled.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_MetadataPersistenceFails_DeletesStoredFileAndRethrows()
    {
        var expectedException = new InvalidOperationException("Database unavailable");
        var repository = new FakeDocumentRepository { AddException = expectedException };
        var fileStore = new FakeDocumentFileStore();
        var service = CreateService(repository, fileStore);

        var act = () => service.UploadAsync(
            new MemoryStream("content"u8.ToArray()),
            "report.md",
            "text/markdown",
            7,
            1024,
            CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(expectedException);
        fileStore.DeletedDocumentId.Should().Be(fileStore.StoredDocumentId);
    }

    private static DocumentUploadService CreateService(
        FakeDocumentRepository repository,
        FakeDocumentFileStore fileStore) =>
        new(
            repository,
            fileStore,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentUploadService>.Instance);
}

internal sealed class FakeDocumentRepository : Rag.Application.Abstractions.IDocumentRepository
{
    public Exception? AddException { get; init; }
    public bool AddCalled { get; private set; }

    public Task<Rag.Domain.Documents.Document> AddAsync(
        Rag.Domain.Documents.Document document,
        CancellationToken cancellationToken = default)
    {
        AddCalled = true;

        return AddException is null
            ? Task.FromResult(document)
            : Task.FromException<Rag.Domain.Documents.Document>(AddException);
    }

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

internal sealed class FakeDocumentFileStore : Rag.Application.Abstractions.IDocumentFileStore
{
    public bool StoreCalled { get; private set; }
    public Guid? StoredDocumentId { get; private set; }
    public Guid? DeletedDocumentId { get; private set; }

    public Task StoreAsync(
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        StoreCalled = true;
        StoredDocumentId = documentId;
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        DeletedDocumentId = documentId;
        return Task.CompletedTask;
    }
}
