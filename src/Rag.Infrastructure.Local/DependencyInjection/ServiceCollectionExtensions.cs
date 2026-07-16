using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rag.Application.Abstractions;
using Rag.Application.Documents;
using Rag.Infrastructure.Local.Health;
using Rag.Infrastructure.Local.Options;
using Rag.Infrastructure.Local.Persistence;
using Rag.Infrastructure.Local.Storage;

namespace Rag.Infrastructure.Local;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DocumentStorageOptions>()
            .Bind(configuration.GetSection(DocumentStorageOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BasePath),
                "DocumentStorage:BasePath is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DatabasePath),
                "DocumentStorage:DatabasePath is required.")
            .ValidateOnStart();

        services.AddSingleton<IDocumentRepository, SqliteDocumentRepository>();
        services.AddSingleton<IDocumentFileStore, LocalDocumentFileStore>();
        services.AddScoped<DocumentUploadService>();

        services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"])
            .AddCheck<LocalFileStorageHealthCheck>("document-storage", tags: ["ready"]);

        return services;
    }
}
