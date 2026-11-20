# ADR-005: Hosted Gemini with local Ollama fallback

- Status: Accepted for the assessment MVP
- Date: 2026-11-20

## Context

D0T2 requires a hosted model, a local model and automatic degradation when the hosted service is unavailable. Embeddings used for offline search must also be available locally.

## Decision

Use Gemini as the configured primary chat provider and Ollama as the default local fallback. Use a shared Application-owned chat contract. Return provider identity and a degraded-mode flag, preserve caller cancellation, and cap provider timeouts. Use Ollama embeddings by default; Gemini embeddings remain an optional online configuration.

## Consequences

- Offline inference requires Ollama and pre-pulled local chat and embedding models. The app cannot download models after the network is disconnected.
- Hosted and local models can differ in latency, token accounting and structured-output reliability.
- Embeddings must be generated and queried with the same provider/model ID. Switching the embedding model requires a full vector-index rebuild.
- Offline capability remains unverified until the full workflow is exercised with internet access disabled on the assessment machine.
