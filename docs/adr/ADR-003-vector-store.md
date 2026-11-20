# ADR-003: Use a local SQLite vector adapter for the MVP

- Status: Accepted for the MVP; production-scale replacement remains possible.
- Date: 2026-11-20

## Context

SQL Server is the requested relational database. The assessment also requires a vector store and T2 offline operation. The MVP must be easy to run locally without a paid managed service, and retrieval needs to work while disconnected from the internet.

## Options considered

1. Store vectors in SQL Server. This keeps data in one server but requires a compatible SQL Server version and vector search capabilities that cannot be assumed on the assessment machine.
2. Use a separate managed vector database. This can scale well but adds an online service, credentials and deployment cost that conflict with a reliable offline demo.
3. Use a local SQLite-backed vector adapter. This is embedded and can run offline; an adapter can be replaced later. A simple exact cosine scan will not scale to a large production corpus.

## Decision

Keep relational records and migrations in SQL Server. Keep vector data behind an Application-owned vector-store contract, implemented initially with a local SQLite file. Store vector dimensions, model identifier and source metadata so incompatible embeddings are not silently mixed. Use deterministic dense ranking; hybrid keyword fusion is added in retrieval phase.

## Consequences

- Local inference and local embeddings can work without an internet service after their model files are present.
- SQLite vector search is suitable only for the assessment corpus and a single-node demo; its scan cost is linear in the candidate set.
- SQL Server remains the authoritative source for document/chunk text and metadata; SQLite is a derived index that can be rebuilt.
- If the corpus or concurrency grows, replace the adapter with an indexed vector service and document its operating requirements.

