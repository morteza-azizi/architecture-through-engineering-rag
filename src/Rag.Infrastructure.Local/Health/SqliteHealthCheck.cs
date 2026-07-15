using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Rag.Infrastructure.Local.Options;

namespace Rag.Infrastructure.Local.Health;

public sealed class SqliteHealthCheck(IOptions<DocumentStorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var databasePath = Path.GetFullPath(options.Value.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                DefaultTimeout = 5,
                Pooling = true
            }.ConnectionString;

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("SQLite is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SQLite is unavailable.", exception);
        }
    }
}
