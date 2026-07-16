using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rag.Domain.Documents;
using Rag.Infrastructure.Local.Options;
using Rag.Infrastructure.Local.Persistence;

namespace Rag.Infrastructure.Tests;

public sealed class SqliteDocumentRepositoryTests : IDisposable
{
    private readonly string _storagePath =
        Path.Combine(Path.GetTempPath(), "rag-sqlite-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AddAndGetByIdAsync_Document_RoundTripsMetadata()
    {
        var repository = CreateRepository();
        var document = CreateDocument();

        await repository.AddAsync(document);
        var stored = await repository.GetByIdAsync(document.Id);

        stored.Should().BeEquivalentTo(document);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingDocument_PersistsStatus()
    {
        var repository = CreateRepository();
        var document = CreateDocument();
        await repository.AddAsync(document);

        await repository.UpdateStatusAsync(document.Id, DocumentStatus.Indexed);
        var stored = await repository.GetByIdAsync(document.Id);

        stored!.Status.Should().Be(DocumentStatus.Indexed);
    }

    [Fact]
    public async Task AddAsync_ConcurrentDocuments_PersistsEveryDocument()
    {
        var repository = CreateRepository();
        var documents = Enumerable.Range(0, 20)
            .Select(_ => CreateDocument())
            .ToArray();

        await Task.WhenAll(documents.Select(document => repository.AddAsync(document)));

        var storedDocuments = await Task.WhenAll(
            documents.Select(document => repository.GetByIdAsync(document.Id)));
        storedDocuments.Should().NotContainNulls();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private SqliteDocumentRepository CreateRepository()
    {
        Directory.CreateDirectory(_storagePath);
        var options = Options.Create(new DocumentStorageOptions
        {
            BasePath = _storagePath,
            DatabasePath = Path.Combine(_storagePath, "rag.db")
        });

        return new SqliteDocumentRepository(
            options,
            NullLogger<SqliteDocumentRepository>.Instance);
    }

    private static Document CreateDocument() =>
        new()
        {
            Id = Guid.NewGuid(),
            FileName = "notes.md",
            ContentType = "text/markdown",
            SizeBytes = 42,
            UploadedAt = DateTimeOffset.UtcNow,
            Status = DocumentStatus.Pending
        };
}
