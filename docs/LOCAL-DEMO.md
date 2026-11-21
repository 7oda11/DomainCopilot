# Local demo

## Requirements

- Docker Desktop with the Linux container engine running.
- An OpenID Connect identity provider that issues JWTs with `Clinician` and `Researcher` roles.
- Network access for the first image/model download; Gemini is optional for the local workflow.

## Windows host with SQL Server LocalDB

Docker is not required to verify the relational schema on Windows when SQL Server LocalDB is installed:

1. Start the instance with `sqllocaldb start MSSQLLocalDB`.
2. The development connection string defaults to `(localdb)\MSSQLLocalDB;Database=DomainCopilot;Trusted_Connection=True;TrustServerCertificate=True`.
3. From the repository root, apply migrations with `dotnet tool restore`, then `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`.
4. Start the API with a real test OIDC authority and audience. The authority is required at startup, even for the anonymous `/health/ready` route.

This path verifies SQL Server migrations and readiness only. Document upload and evaluation still require an embedding provider; offline evaluation requires Ollama and its models to be installed and pulled first.

## Start

1. Copy `.env.example` to `.env` and replace the sample SQL password and identity-provider authority/audience values.
2. Run `docker compose up --build -d`.
3. While online, run `./scripts/pull-models.ps1` to pull `qwen3:8b` and `nomic-embed-text` into the persistent Ollama volume.
4. The Compose API service applies the SQL migrations on startup. In non-Compose environments, apply them explicitly with the repository's pinned EF tool: `dotnet tool restore`, then `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`.
5. Use the OpenAPI document at `/openapi/v1.json` and pass a JWT with the required role.

## Demo sequence

1. `POST /api/cases` with `{ "syntheticCaseSummary": "Synthetic test scenario only" }` as a `Clinician`.
2. Set a valid role token as `DOMAINCOPILOT_BEARER_TOKEN` and run `./scripts/ingest-synthetic-corpus.ps1` to index the included 30-document synthetic corpus, or upload one public/synthetic PDF or DOCX through `POST /api/documents`.
3. `POST /api/copilot/drafts` with the returned `caseId` and a question.
4. Inspect returned evidence and progress; only a successful safety review returns `AwaitingApproval` and `draftId`.
5. `POST /api/drafts/{draftId}/review` with an approve, reject, or edited-and-approved action as a `Clinician`.

For a local-only model run set `AI_PRIMARY_PROVIDER=ollama` in `.env`, keep `EMBEDDING_PROVIDER=ollama`, pre-pull both models, and then disconnect internet access. A synthetic draft was exercised through all three local Qwen agents and reached `AwaitingApproval`; the test kept internet available. The complete disconnected run has not yet been exercised. The OIDC authority itself must also be reachable or configured with a local identity provider for offline authentication.

## Boundaries

The 30-document / 150-page synthetic corpus is bundled and was indexed into SQL Server LocalDB and SQLite using Ollama Nomic embeddings. The live retrieval baseline is recorded in `docs/EVALUATION.md`; it measures retrieval and lexical support, not generated-answer safety. Local Qwen generated a synthetic draft that reached the clinician approval gate. A disconnected end-to-end run remains unverified. OCR, answer-level groundedness, persistent agent traces, a reusable local identity-provider fixture and production EHR publishing remain open tasks. No real medical data is included. Do not use this MVP for patient care.
