using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rag.Infrastructure.Local.Options;
using Rag.Infrastructure.Local.Storage;

namespace Rag.Infrastructure.Tests;

public sealed class LocalDocumentFileStoreTests : IDisposable
{
    private readonly string _storagePath =
        Path.Combine(Path.GetTempPath(), "rag-file-store-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task StoreAndOpenReadAsync_SafeFileName_RoundTripsContent()
    {
        var store = CreateStore();
        var documentId = Guid.NewGuid();
        await using var content = new MemoryStream("hello rag"u8.ToArray());

        await store.StoreAsync(documentId, "notes.md", content);
        await using var storedContent = await store.OpenReadAsync(documentId);
        using var reader = new StreamReader(storedContent, Encoding.UTF8);

        var text = await reader.ReadToEndAsync();

        text.Should().Be("hello rag");
    }

    [Theory]
    [InlineData("../escape.md")]
    [InlineData("folder/escape.md")]
    [InlineData("folder\\escape.md")]
    [InlineData("/tmp/escape.md")]
    public async Task StoreAsync_PathInFileName_RejectsWrite(string fileName)
    {
        var store = CreateStore();
        await using var content = new MemoryStream("unsafe"u8.ToArray());

        var act = () => store.StoreAsync(Guid.NewGuid(), fileName, content);

        await act.Should().ThrowAsync<ArgumentException>();
        Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_StoredDocument_RemovesDocumentDirectory()
    {
        var store = CreateStore();
        var documentId = Guid.NewGuid();
        await using var content = new MemoryStream("content"u8.ToArray());
        await store.StoreAsync(documentId, "notes.txt", content);

        await store.DeleteAsync(documentId);

        var documentDirectory = Path.Combine(_storagePath, "documents", documentId.ToString());
        Directory.Exists(documentDirectory).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private LocalDocumentFileStore CreateStore()
    {
        var options = Options.Create(new DocumentStorageOptions
        {
            BasePath = _storagePath,
            DatabasePath = Path.Combine(_storagePath, "rag.db")
        });

        return new LocalDocumentFileStore(
            options,
            NullLogger<LocalDocumentFileStore>.Instance);
    }
}
