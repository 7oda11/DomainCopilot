# Domain Copilot

Healthcare Domain Copilot for assessment variant D0T2. This repository contains a Clean Architecture foundation, core healthcare workflow models, SQL Server persistence, PDF/DOCX ingestion, hybrid evidence retrieval, and Gemini-to-Ollama provider adapters.

## Architecture

```text
DomainCopilot.Api -> DomainCopilot.Application -> DomainCopilot.Domain
                 -> DomainCopilot.Infrastructure -> Application + Domain
```

`Domain` owns business rules. `Application` owns use cases and ports. `Infrastructure` implements SQL Server, document extraction, provider adapters and a local SQLite vector index. `Api` is the composition root and HTTP boundary. Domain and Application must not reference Infrastructure, web frameworks, provider SDKs or vector-store SDKs.

## Submission assets

Use this map to find each assessment deliverable:

| Deliverable | Location |
|---|---|
| Public GitHub repository (keep public for 30 days) | [github.com/7oda11/DomainCopilot](https://github.com/7oda11/DomainCopilot) |
| Consolidated submission guide | [Markdown guide](docs/SUBMISSION-PACK.md) · [Word guide](docs/SUBMISSION-PACK-GUIDE.docx) |
| BRD: context, personas, measurable objectives, requirements, scope, risks, traceability | [docs/BRD.md](docs/BRD.md) |
| System Design: target architecture, implemented MVP, gaps, tradeoffs | [docs/SYSTEM-DESIGN.md](docs/SYSTEM-DESIGN.md) |
| C4 diagrams, workflow sequence, trust boundaries, ER/layer views | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| Architecture decisions (ADRs) | [docs/adr/](docs/adr/) |
| Security report | [docs/SECURITY.md](docs/SECURITY.md) |
| Evaluation methodology and measured results | [docs/EVALUATION.md](docs/EVALUATION.md) · [golden cases](tests/DomainCopilot.Evaluation/evaluation-cases.json) · [baseline report](docs/reports/retrieval-baseline-2026-11-21.json) |
| Offline/degraded-mode checklist and run evidence | [docs/OFFLINE-VERIFICATION.md](docs/OFFLINE-VERIFICATION.md) · [offline workflow report](docs/reports/offline-workflow-2026-11-25.md) |
| AI-use record | [docs/AI-USAGE-LOG.md](docs/AI-USAGE-LOG.md) |
| 90-minute teaching plan, lab, answers/challenges, outcomes and misconceptions | [teaching/](teaching/) |
| Project overview deck (22 slides) | [teaching/presentations/HealthCareCopilot-Project-Overview.pptx](teaching/presentations/HealthCareCopilot-Project-Overview.pptx) |
| Short RAG explainer (9 slides) | [teaching/presentations/HealthCareCopilot-RAG-Explainer.pptx](teaching/presentations/HealthCareCopilot-RAG-Explainer.pptx) |
| Project demo video | [Google Drive video](https://drive.google.com/file/d/1xEalHhp6k-2rfZAStlbY3ATT95uxAdYP/view?usp=sharing) |
| Teaching sample video | [Google Drive video](https://drive.google.com/file/d/1PQhmXfyajqlgaqX5ay1ptNMiFuJJ-CJD/view?usp=sharing) |
| Docker setup and local configuration | [Dockerfile](Dockerfile) · [docker-compose.yml](docker-compose.yml) · [.env.example](.env.example) |
| Database migrations, synthetic documents and case examples | [EF Core migrations](src/DomainCopilot.Infrastructure/Migrations/) · [docs/samples/](docs/samples/) · [30-document corpus](data/synthetic/) |
| Test and CI setup | [tests/](tests/) · [GitHub Actions](.github/workflows/) |

**5-minute demo path:** register/login → upload a synthetic document → create a case → ask a supported question → inspect citations and trace → show Researcher review denied (403) → approve as Clinician. See [docs/LOCAL-DEMO.md](docs/LOCAL-DEMO.md) for the full runbook and current limitations.

## Current API slices

- `POST /api/documents`: accept text PDF or DOCX, extract and chunk content, persist source records in SQL Server, and create embedding vectors in SQLite.
- `POST /api/search`: run dense and keyword retrieval, fuse the rankings with RRF, and return chunk-level citations or an explicit no-evidence result.
- `POST /api/cases`: create a synthetic case summary (Researcher or Clinician).
- `POST /api/copilot/drafts`: run the three-agent pipeline and save a draft for clinician review; `/stream` returns SSE progress.
- `POST /api/drafts/{draftId}/review`: approve, reject or edit-and-approve (Clinician role).

The API provides `POST /api/auth/register` and `POST /api/auth/login`. Registration creates a `Researcher` account; login returns a signed bearer JWT. Passwords are stored as ASP.NET Identity password hashes. Configure `Authentication:SigningKey` as a secret of at least 32 bytes, plus the issuer and audience, before starting the API. For local development, store the signing key with `dotnet user-secrets`; never commit it.

Run `dotnet run --project src/DomainCopilot.Api --launch-profile http` to start the API and open Swagger UI at `http://localhost:5161/swagger`. The importable Postman collection is [`postman/DomainCopilot.postman_collection.json`](postman/DomainCopilot.postman_collection.json); select an environment, register, then log in to save the bearer token.

Before first run, apply the database schema with `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`. To create a local Clinician demo account, follow [`docs/ROLE-PROVISIONING.md`](docs/ROLE-PROVISIONING.md).

The standalone Angular workspace for **HealthCareCopilot** is in [`web/`](web/README.md). Start the API, then run `npm install` and `npm start` from `web/`; the UI opens at `http://localhost:4200` and proxies `/api` calls to the API on port 5161. Both roles can search and generate drafts; only `Clinician` can review/approve them. `Researcher` can upload documents.

Readable, entirely fictional sample documents for local retrieval are in [`docs/samples/`](docs/samples/README.md). Do not use these examples as clinical guidance.

## Synthetic assessment corpus

The repository includes 30 generated, fictional workflow PDFs with five explicit pages each (150 pages total). Regenerate them with `python scripts/generate_synthetic_corpus.py` (requires `reportlab`); the script writes a SHA-256 manifest. To index them, start the API and SQL Server, then set `DOMAINCOPILOT_API_URL` and `DOMAINCOPILOT_BEARER_TOKEN` and run `./scripts/ingest-synthetic-corpus.ps1` in PowerShell 7. No real patient data is included. These documents are engineering fixtures, not clinical guidance.

Chat-provider adapters, Gemini-to-Ollama failover, three agent stages, tool allow-lists, an approval queue, a JWT-protected API and an API-based evaluation harness are implemented. The 30-document synthetic corpus was indexed through the authenticated API using SQL Server LocalDB and local Nomic embeddings; the recorded retrieval baseline has a 100% source hit rate, 100% lexical groundedness proxy and 72% search refusal-flag agreement. A synthetic draft completed the three-agent workflow using local Qwen and reached `AwaitingApproval`. A full workflow also completed with the API's external HTTP egress isolated to an unavailable loopback proxy; system-wide network disconnection remains unverified. See the [offline verification report](docs/reports/offline-workflow-2026-11-25.md). Durable completed-agent traces and workspace ownership migrations are included and must be applied on a fresh database. An actual EHR integration remains incomplete. Read the [offline verification checklist](docs/OFFLINE-VERIFICATION.md), [local demo guide](docs/LOCAL-DEMO.md), [evaluation methodology](docs/EVALUATION.md), [security notes](docs/SECURITY.md), [ingestion guide](docs/INGESTION.md), [retrieval strategy](docs/RETRIEVAL.md) and [provider setup](docs/PROVIDERS.md) before running locally.

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
