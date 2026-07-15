using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rag.Infrastructure.Local.Options;

namespace Rag.Infrastructure.Local.Health;

public sealed class LocalFileStorageHealthCheck(
    IOptions<DocumentStorageOptions> options,
    ILogger<LocalFileStorageHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? probePath = null;

        try
        {
            var storagePath = Path.GetFullPath(options.Value.BasePath);
            Directory.CreateDirectory(storagePath);
            probePath = Path.Combine(storagePath, $".health-{Guid.NewGuid():N}");

            await File.WriteAllTextAsync(probePath, string.Empty, cancellationToken);
            File.Delete(probePath);

            return HealthCheckResult.Healthy("Local document storage is writable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Local document storage is unavailable.", exception);
        }
        finally
        {
            if (probePath is not null)
            {
                try
                {
                    File.Delete(probePath);
                }
                catch (Exception cleanupException)
                {
                    logger.LogWarning(cleanupException, "Failed to remove file storage health-check probe");
                }
            }
        }
    }
}
