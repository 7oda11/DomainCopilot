using DomainCopilot.Infrastructure.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace DomainCopilot.Infrastructure.Tests;

public sealed class SqliteVectorIndexTests
{
    [Fact]
    public async Task Search_returns_nearest_vector_and_upsert_replaces_existing_vector()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"domain-copilot-{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VectorStore:SqlitePath"] = databasePath
        }).Build();
        var index = new SqliteVectorIndex(configuration);
        var nearest = Guid.NewGuid();
        var other = Guid.NewGuid();
        try
        {
            await index.UpsertAsync(nearest, "local-v1", [1, 0], CancellationToken.None);
            await index.UpsertAsync(other, "local-v1", [0, 1], CancellationToken.None);
            await index.UpsertAsync(nearest, "local-v1", [0.8f, 0.2f], CancellationToken.None);

            var results = await index.SearchAsync("local-v1", [1, 0], 2, CancellationToken.None);

            Assert.Equal(nearest, results[0].ChunkId);
            Assert.Equal(2, results.Count);
            Assert.True(results[0].Similarity > results[1].Similarity);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
