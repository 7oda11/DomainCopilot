using DomainCopilot.Application.Abstractions.Persistence;
using DomainCopilot.Application.Abstractions.Documents;
using DomainCopilot.Application.Features.Ingestion;
using DomainCopilot.Application.Abstractions.Retrieval;
using DomainCopilot.Application.Abstractions.AI;
using DomainCopilot.Application.AI;
using DomainCopilot.Application.Features.Retrieval;
using DomainCopilot.Infrastructure.AI;
using DomainCopilot.Infrastructure.Documents;
using DomainCopilot.Infrastructure.Persistence;
using DomainCopilot.Infrastructure.Repositories;
using DomainCopilot.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DomainCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IDocumentIngestionStore, EfDocumentIngestionStore>();
        services.AddScoped<IDocumentRetrievalStore, EfDocumentRetrievalStore>();
        services.AddScoped<IDocumentTextExtractor, PdfPigTextExtractor>();
        services.AddScoped<IDocumentTextExtractor, OpenXmlDocxTextExtractor>();
        services.AddHttpClient();
        services.AddHttpClient(nameof(GeminiEmbeddingGenerator), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient(nameof(OllamaEmbeddingGenerator), client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient(nameof(GeminiChatCompletionProvider), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient(nameof(OllamaChatCompletionProvider), client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddScoped<OllamaEmbeddingGenerator>();
        services.AddScoped<GeminiEmbeddingGenerator>();
        services.AddScoped<GeminiChatCompletionProvider>();
        services.AddScoped<OllamaChatCompletionProvider>();
        services.AddScoped<IEmbeddingGenerator>(provider =>
        {
            var selected = configuration["Embedding:Provider"] ?? "ollama";
            return selected.ToLowerInvariant() switch
            {
                "ollama" => provider.GetRequiredService<OllamaEmbeddingGenerator>(),
                "gemini" => provider.GetRequiredService<GeminiEmbeddingGenerator>(),
                _ => throw new InvalidOperationException($"Unknown embedding provider '{selected}'. Use 'ollama' or 'gemini'.")
            };
        });
        services.AddScoped<IChatCompletionService>(provider =>
        {
            var primaryName = configuration["AI:PrimaryProvider"] ?? "gemini";
            var fallbackName = configuration["AI:FallbackProvider"] ?? "ollama";
            var primary = ResolveChatProvider(provider, primaryName);
            var fallback = ResolveChatProvider(provider, fallbackName);
            return new FallbackChatCompletionService(primary, fallback,
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FallbackChatCompletionService>>());
        });
        services.AddSingleton<IVectorIndex, SqliteVectorIndex>();
        services.AddSingleton<DocumentChunker>();
        services.AddScoped<IngestDocumentHandler>();
        services.AddScoped<HybridSearchHandler>();

        return services;
    }

    private static IChatCompletionProvider ResolveChatProvider(IServiceProvider provider, string name) => name.ToLowerInvariant() switch
    {
        "gemini" => provider.GetRequiredService<GeminiChatCompletionProvider>(),
        "ollama" => provider.GetRequiredService<OllamaChatCompletionProvider>(),
        _ => throw new InvalidOperationException($"Unknown chat provider '{name}'. Use 'gemini' or 'ollama'.")
    };
}
