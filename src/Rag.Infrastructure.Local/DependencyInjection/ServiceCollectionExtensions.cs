using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rag.Application.Abstractions;
using Rag.Application.Documents;
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
        services.Configure<DocumentStorageOptions>(
            configuration.GetSection(DocumentStorageOptions.SectionName));

        services.AddSingleton<IDocumentRepository, SqliteDocumentRepository>();
        services.AddSingleton<IDocumentFileStore, LocalDocumentFileStore>();
        services.AddScoped<DocumentUploadService>();

        return services;
    }
}
