using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rag.Application.Abstractions;
using Rag.Domain.Documents;
using Rag.Infrastructure.Local.Options;

namespace Rag.Infrastructure.Local.Persistence;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDocumentRepository> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteDocumentRepository(
        IOptions<DocumentStorageOptions> options,
        ILogger<SqliteDocumentRepository> logger)
    {
        var databasePath = Path.GetFullPath(options.Value.DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            DefaultTimeout = 5
        }.ConnectionString;
        _logger = logger;
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Documents (Id, FileName, ContentType, SizeBytes, UploadedAt, Status)
            VALUES ($id, $fileName, $contentType, $sizeBytes, $uploadedAt, $status);
            """;
        command.Parameters.AddWithValue("$id", document.Id.ToString());
        command.Parameters.AddWithValue("$fileName", document.FileName);
        command.Parameters.AddWithValue("$contentType", document.ContentType);
        command.Parameters.AddWithValue("$sizeBytes", document.SizeBytes);
        command.Parameters.AddWithValue(
            "$uploadedAt",
            document.UploadedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", document.Status.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Persisted document metadata {DocumentId}", document.Id);

        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, FileName, ContentType, SizeBytes, UploadedAt, Status
            FROM Documents
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapDocument(reader);
    }

    public async Task UpdateStatusAsync(Guid id, DocumentStatus status, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Documents
            SET Status = $status
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$status", status.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Updated document {DocumentId} status to {Status}", id, status);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;

                CREATE TABLE IF NOT EXISTS Documents (
                    Id TEXT PRIMARY KEY,
                    FileName TEXT NOT NULL,
                    ContentType TEXT NOT NULL,
                    SizeBytes INTEGER NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    Status TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
            _logger.LogInformation("SQLite document repository initialized");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static Document MapDocument(SqliteDataReader reader)
    {
        var statusValue = reader.GetString(5);
        if (!Enum.TryParse<DocumentStatus>(statusValue, ignoreCase: false, out var status))
        {
            throw new InvalidDataException($"Stored document status '{statusValue}' is invalid.");
        }

        return new Document
        {
            Id = Guid.Parse(reader.GetString(0)),
            FileName = reader.GetString(1),
            ContentType = reader.GetString(2),
            SizeBytes = reader.GetInt64(3),
            UploadedAt = DateTimeOffset.Parse(
                reader.GetString(4),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
            Status = status
        };
    }
}
