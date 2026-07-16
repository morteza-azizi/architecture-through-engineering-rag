using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rag.Infrastructure.Local.Health;
using Rag.Infrastructure.Local.Options;

namespace Rag.Infrastructure.Tests;

public sealed class LocalInfrastructureHealthCheckTests : IDisposable
{
    private readonly string _storagePath =
        Path.Combine(Path.GetTempPath(), "rag-health-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task HealthChecks_AvailableStorage_ReturnHealthy()
    {
        var options = CreateOptions(_storagePath, Path.Combine(_storagePath, "rag.db"));
        var fileCheck = new LocalFileStorageHealthCheck(
            options,
            NullLogger<LocalFileStorageHealthCheck>.Instance);
        var sqliteCheck = new SqliteHealthCheck(options);

        var fileResult = await fileCheck.CheckHealthAsync(new HealthCheckContext());
        var sqliteResult = await sqliteCheck.CheckHealthAsync(new HealthCheckContext());

        fileResult.Status.Should().Be(HealthStatus.Healthy);
        sqliteResult.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthChecks_PathBlockedByFile_ReturnUnhealthy()
    {
        Directory.CreateDirectory(_storagePath);
        var blockingFile = Path.Combine(_storagePath, "blocked");
        await File.WriteAllTextAsync(blockingFile, "not a directory");

        var options = CreateOptions(
            Path.Combine(blockingFile, "documents"),
            Path.Combine(blockingFile, "rag.db"));
        var fileCheck = new LocalFileStorageHealthCheck(
            options,
            NullLogger<LocalFileStorageHealthCheck>.Instance);
        var sqliteCheck = new SqliteHealthCheck(options);

        var fileResult = await fileCheck.CheckHealthAsync(new HealthCheckContext());
        var sqliteResult = await sqliteCheck.CheckHealthAsync(new HealthCheckContext());

        fileResult.Status.Should().Be(HealthStatus.Unhealthy);
        sqliteResult.Status.Should().Be(HealthStatus.Unhealthy);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private static IOptions<DocumentStorageOptions> CreateOptions(
        string basePath,
        string databasePath) =>
        Options.Create(new DocumentStorageOptions
        {
            BasePath = basePath,
            DatabasePath = databasePath
        });
}
