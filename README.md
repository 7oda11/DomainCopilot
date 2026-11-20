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
- `POST /api/cases`: create a case summary for the demo (Clinician role).
- `POST /api/copilot/drafts`: run the three-agent pipeline and save a draft for clinician review; `/stream` returns SSE progress.
- `POST /api/drafts/{draftId}/review`: approve, reject or edit-and-approve (Clinician role).

The API requires a JWT identity provider configured through `Authentication__Authority` and `Authentication__Audience`. For local startup set these in the environment; the placeholder in `appsettings.json` intentionally does not point to a real tenant.

Chat-provider adapters, Gemini-to-Ollama failover, three agent stages, tool allow-lists, an approval queue and JWT-protected API are implemented. The assessment corpus floor, evaluation harness, a disconnected end-to-end run, durable per-agent traces and an actual EHR integration remain incomplete. Read the [local demo guide](docs/LOCAL-DEMO.md), [security notes](docs/SECURITY.md), [ingestion guide](docs/INGESTION.md), [retrieval strategy](docs/RETRIEVAL.md) and [provider setup](docs/PROVIDERS.md) before running locally.

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
