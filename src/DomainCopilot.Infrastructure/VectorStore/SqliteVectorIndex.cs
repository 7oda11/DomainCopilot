using System.Buffers.Binary;
using DomainCopilot.Application.Abstractions.Retrieval;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace DomainCopilot.Infrastructure.VectorStore;

public sealed class SqliteVectorIndex : IVectorIndex
{
    private readonly string _databasePath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public SqliteVectorIndex(IConfiguration configuration)
    {
        _databasePath = Path.GetFullPath(configuration["VectorStore:SqlitePath"] ?? "App_Data/domaincopilot-vectors.db");
    }

    public async Task UpsertAsync(Guid chunkId, string modelId, float[] vector, CancellationToken cancellationToken)
    {
        ValidateVector(vector);
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO vectors(chunk_id, model_id, dimensions, vector)
            VALUES ($chunk, $model, $dimensions, $vector)
            ON CONFLICT(chunk_id) DO UPDATE SET model_id=excluded.model_id, dimensions=excluded.dimensions, vector=excluded.vector;
            """;
        command.Parameters.AddWithValue("$chunk", chunkId.ToString("N"));
        command.Parameters.AddWithValue("$model", modelId);
        command.Parameters.AddWithValue("$dimensions", vector.Length);
        command.Parameters.AddWithValue("$vector", Encode(vector));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorMatch>> SearchAsync(string modelId, float[] queryVector, int limit, CancellationToken cancellationToken)
    {
        ValidateVector(queryVector);
        if (limit is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(limit));
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT chunk_id, vector FROM vectors WHERE model_id=$model AND dimensions=$dimensions";
        command.Parameters.AddWithValue("$model", modelId);
        command.Parameters.AddWithValue("$dimensions", queryVector.Length);
        var matches = new List<VectorMatch>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var chunkId = Guid.ParseExact(reader.GetString(0), "N");
                var vector = Decode((byte[])reader[1], queryVector.Length);
                matches.Add(new VectorMatch(chunkId, CosineSimilarity(queryVector, vector)));
            }
        }

        return matches.OrderByDescending(match => match.Similarity).Take(limit).ToArray();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS vectors(
                    chunk_id TEXT PRIMARY KEY,
                    model_id TEXT NOT NULL,
                    dimensions INTEGER NOT NULL,
                    vector BLOB NOT NULL);
                CREATE INDEX IF NOT EXISTS ix_vectors_model_dimensions ON vectors(model_id, dimensions);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static byte[] Encode(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        for (var index = 0; index < vector.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * sizeof(float)), BitConverter.SingleToInt32Bits(vector[index]));
        }

        return bytes;
    }

    private static float[] Decode(byte[] bytes, int dimensions)
    {
        if (bytes.Length != dimensions * sizeof(float)) throw new InvalidDataException("Stored vector dimensions are corrupt.");
        var vector = new float[dimensions];
        for (var index = 0; index < dimensions; index++)
        {
            vector[index] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index * sizeof(float))));
        }

        return vector;
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        return leftNorm == 0 || rightNorm == 0 ? 0 : dot / Math.Sqrt(leftNorm * rightNorm);
    }

    private static void ValidateVector(float[] vector)
    {
        if (vector.Length == 0 || vector.Length > 16384 || vector.Any(value => !float.IsFinite(value)))
        {
            throw new ArgumentException("A finite, non-empty embedding vector is required.", nameof(vector));
        }
    }
}
