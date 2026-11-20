# AI providers and degraded mode

## Configuration

- `AI:PrimaryProvider=gemini` selects the hosted chat provider.
- `AI:FallbackProvider=ollama` selects local chat when Gemini is unavailable, times out, returns malformed data, or has no configured API key.
- `Embedding:Provider=ollama` selects local embeddings by default. Gemini embeddings can be selected with `Embedding:Provider=gemini` and `GEMINI_API_KEY`.
- `AI:Ollama:BaseUrl` defaults to `http://localhost:11434`; the local chat model defaults to `qwen3:8b`, and the embedding model to `nomic-embed-text`.
- Hosted provider keys come from environment variables and must not be committed.

## Runtime behavior

`IChatCompletionService` returns the provider ID and `IsDegraded`. The fallback logs the primary-provider failure and only treats timeout/network/provider-unavailable or malformed-provider responses as failover conditions. Caller cancellation is passed through and does not start another model request. Gemini has a 20-second timeout and local Ollama has a 90-second timeout.

Local operation requires Ollama to be installed, both local models to be present before disconnecting, and SQL Server plus the local SQLite index to be available. The code path is implemented; an offline claim is not verified until an end-to-end ingestion and search run succeeds with internet blocked on the target machine. Local model answers may differ in structured-output quality and speed from Gemini.
