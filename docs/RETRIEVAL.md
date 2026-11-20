# Hybrid retrieval

`POST /api/search` uses two independent retrievers over the same indexed chunks:

1. Ollama creates the query embedding (default local model: `nomic-embed-text`). SQLite scans vectors for that model and exact cosine similarity. This exact scan is deterministic and suitable for the assessment corpus, not a large multi-tenant production index.
2. SQL Server keyword search matches query terms against chunk text.
3. Reciprocal Rank Fusion combines the ranked lists using `1 / (60 + rank)`. Rank fusion avoids assuming that cosine and keyword scores share a scale.
4. The relational store loads matching chunks and metadata. Each result becomes an `EvidenceCitation` whose quote must exist in its cited chunk.

If neither retriever returns a usable result, the API marks the result as refused and returns no evidence. This endpoint is evidence search only; a later answer-generation workflow must treat the evidence as untrusted input and must not infer clinical recommendations from retrieval scores alone.

## Embedding consistency

The selected provider and model ID are stored with each vector. Search queries use the configured embedding model, and the SQLite query filters by model ID and vector dimension so vectors from incompatible models are not compared. Use the same provider/model for ingestion and search. Changing models requires re-embedding and rebuilding the derived SQLite index.

Set `Embedding:Provider=ollama` for offline embeddings. `Embedding:Provider=gemini` requires `GEMINI_API_KEY` (or `Embedding:Gemini:ApiKey`) and internet access; it cannot run offline.
