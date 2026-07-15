using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Rag.Api.Contracts;

namespace Rag.Api.IntegrationTests;

public sealed class DocumentUploadTests : IClassFixture<RagWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _storagePath;

    public DocumentUploadTests(RagWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _storagePath = factory.StoragePath;
    }

    [Fact]
    public async Task UploadMarkdown_ReturnsCreatedWithMetadata()
    {
        using var content = CreateFileContent("guide.md", "# Architecture\n\nClean boundaries matter.");

        var response = await _client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        document.Should().NotBeNull();
        document!.FileName.Should().Be("guide.md");
        document.Status.Should().Be("Pending");
        document.SizeBytes.Should().BeGreaterThan(0);

        var storedDirectory = Path.Combine(_storagePath, "documents", document.Id.ToString());
        Directory.Exists(storedDirectory).Should().BeTrue();
        File.Exists(Path.Combine(storedDirectory, "guide.md")).Should().BeTrue();
    }

    [Fact]
    public async Task UploadPdf_ReturnsBadRequest()
    {
        using var content = CreateFileContent("notes.pdf", "%PDF-1.4 fake");

        var response = await _client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadEmptyFile_ReturnsBadRequest()
    {
        using var content = CreateFileContent("empty.txt", string.Empty);

        var response = await _client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadRequestExceedingConfiguredLimit_ReturnsPayloadTooLarge()
    {
        using var content = CreateFileContent(
            "large.txt",
            new byte[11 * 1024 * 1024]);

        var response = await _client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task UploadFileNameContainingPath_ReturnsBadRequestWithoutWritingFile()
    {
        using var content = CreateFileContent("../escape.md", "unsafe");

        var response = await _client.PostAsync("/api/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories)
            .Should().NotContain(path => path.EndsWith("escape.md", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetById_AfterUpload_ReturnsDocument()
    {
        using var uploadContent = CreateFileContent("readme.txt", "Hello RAG");
        var uploadResponse = await _client.PostAsync("/api/documents", uploadContent);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentResponse>();

        var response = await _client.GetAsync($"/api/documents/{uploaded!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>();
        document!.FileName.Should().Be("readme.txt");
    }

    [Fact]
    public async Task GetById_UnknownDocument_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static MultipartFormDataContent CreateFileContent(string fileName, string text)
        => CreateFileContent(fileName, System.Text.Encoding.UTF8.GetBytes(text));

    private static MultipartFormDataContent CreateFileContent(string fileName, byte[] bytes)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }
}

public sealed class RagWebApplicationFactory : WebApplicationFactory<Program>
{
    public string StoragePath { get; } = Path.Combine(Path.GetTempPath(), "rag-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        Directory.CreateDirectory(StoragePath);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DocumentStorage:BasePath"] = StoragePath,
                ["DocumentStorage:DatabasePath"] = Path.Combine(StoragePath, "rag.db"),
                ["DocumentUpload:MaxFileSizeBytes"] = "10485760"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(StoragePath))
        {
            Directory.Delete(StoragePath, recursive: true);
        }

        base.Dispose(disposing);
    }
}
