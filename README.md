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

The API provides `POST /api/auth/register` and `POST /api/auth/login`. Registration creates a `Researcher` account; login returns a signed bearer JWT. Passwords are stored as ASP.NET Identity password hashes. Configure `Authentication:SigningKey` as a secret of at least 32 bytes, plus the issuer and audience, before starting the API. For local development, store the signing key with `dotnet user-secrets`; never commit it.

Run `dotnet run --project src/DomainCopilot.Api --launch-profile http` to start the API and open Swagger UI at `http://localhost:5161/swagger`. The importable Postman collection is [`postman/DomainCopilot.postman_collection.json`](postman/DomainCopilot.postman_collection.json); select an environment, register, then log in to save the bearer token.

## Synthetic assessment corpus

The repository includes 30 generated, fictional workflow PDFs with five explicit pages each (150 pages total). Regenerate them with `python scripts/generate_synthetic_corpus.py` (requires `reportlab`); the script writes a SHA-256 manifest. To index them, start the API and SQL Server, then set `DOMAINCOPILOT_API_URL` and `DOMAINCOPILOT_BEARER_TOKEN` and run `./scripts/ingest-synthetic-corpus.ps1` in PowerShell 7. No real patient data is included. These documents are engineering fixtures, not clinical guidance.

Chat-provider adapters, Gemini-to-Ollama failover, three agent stages, tool allow-lists, an approval queue, a JWT-protected API and an API-based evaluation harness are implemented. The 30-document synthetic corpus was indexed through the authenticated API using SQL Server LocalDB and local Nomic embeddings; the recorded retrieval baseline has a 100% source hit rate, 100% lexical groundedness proxy and 72% search refusal-flag agreement. A synthetic draft also completed the three-agent workflow using local Qwen and reached `AwaitingApproval`. A fully disconnected run, durable per-agent traces and an actual EHR integration remain incomplete. Read the [local demo guide](docs/LOCAL-DEMO.md), [evaluation methodology](docs/EVALUATION.md), [security notes](docs/SECURITY.md), [ingestion guide](docs/INGESTION.md), [retrieval strategy](docs/RETRIEVAL.md) and [provider setup](docs/PROVIDERS.md) before running locally.

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
