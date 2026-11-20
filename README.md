# Domain Copilot

Healthcare Domain Copilot for assessment variant D0T2. This repository contains a Clean Architecture foundation, core healthcare workflow models, SQL Server persistence, PDF/DOCX ingestion, hybrid evidence retrieval, and Gemini-to-Ollama provider adapters.

## Architecture

```text
DomainCopilot.Api -> DomainCopilot.Application -> DomainCopilot.Domain
                 -> DomainCopilot.Infrastructure -> Application + Domain
```

`Domain` owns business rules. `Application` owns use cases and ports. `Infrastructure` implements SQL Server, document extraction, provider adapters and a local SQLite vector index. `Api` is the composition root and HTTP boundary. Domain and Application must not reference Infrastructure, web frameworks, provider SDKs or vector-store SDKs.

## Current API slices

- `POST /api/documents`: accept text PDF or DOCX, extract and chunk content, persist source records in SQL Server, and create embedding vectors in SQLite.
- `POST /api/search`: run dense and keyword retrieval, fuse the rankings with RRF, and return chunk-level citations or an explicit no-evidence result.

Chat-provider adapters and automatic Gemini-to-Ollama failover are implemented behind `IChatCompletionService`; a clinician-facing answer/approval workflow and verified disconnected end-to-end run are still in progress. Read [the ingestion guide](docs/INGESTION.md), [retrieval strategy](docs/RETRIEVAL.md), and [provider setup](docs/PROVIDERS.md) before running locally.

## Prerequisites

- .NET 10 SDK 10.0.404 or a later 10.0 patch.

The local development environment used a portable copy of SDK 10.0.404 from the earlier workspace project. Install the SDK normally on a clean machine; the portable SDK is not copied into this repository.

## Build and verify

From this directory:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

The `global.json` pins the SDK feature band used for the project. Run instructions are documented in the links above; SQL Server, Ollama and the configured model weights are required for an end-to-end local demo.
