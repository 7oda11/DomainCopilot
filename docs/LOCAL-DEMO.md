# Local demo

## Requirements

- Docker Desktop with the Linux container engine running.
- A local JWT signing key of at least 32 bytes stored in .NET User Secrets.
- Network access for the first image/model download; Gemini is optional for the local workflow.

## Windows host with SQL Server LocalDB

Docker is not required to verify the relational schema on Windows when SQL Server LocalDB is installed:

1. Start the instance with `sqllocaldb start MSSQLLocalDB`.
2. The development connection string defaults to `(localdb)\MSSQLLocalDB;Database=DomainCopilot;Trusted_Connection=True;TrustServerCertificate=True`.
3. From the repository root, apply migrations with `dotnet tool restore`, then `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`.
4. Configure a local JWT signing key with `dotnet user-secrets set "Authentication:SigningKey" "<random-secret-at-least-32-bytes>" --project src/DomainCopilot.Api`. The development issuer and audience default to `DomainCopilot` and `domain-copilot-api`.

This path verifies SQL Server migrations and readiness only. Document upload and evaluation still require an embedding provider; offline evaluation requires Ollama and its models to be installed and pulled first.

## Start

1. Copy `.env.example` to `.env`, replace the sample SQL password, and set a high-entropy `AUTHENTICATION_SIGNING_KEY` of at least 32 bytes. Keep `.env` untracked.
2. Run `docker compose up --build -d`.
3. While online, run `./scripts/pull-models.ps1` to pull `qwen3:8b` and `nomic-embed-text` into the persistent Ollama volume.
4. The Compose API service applies the SQL migrations on startup. In non-Compose environments, apply them explicitly with the repository's pinned EF tool: `dotnet tool restore`, then `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`.
5. Register with `POST /api/auth/register`, then log in with `POST /api/auth/login` to receive a bearer JWT. Public registration creates a `Researcher`; a trusted administrator must provision `Clinician` access separately.

## Local registration and login

Run the API from the repository root with `dotnet run --project src/DomainCopilot.Api --launch-profile http`; the launch profile opens Swagger UI at `http://localhost:5161/swagger`. Send `POST http://localhost:5161/api/auth/register` with `{"email":"researcher@example.test","password":"Use-a-unique-password-123"}`, then send the same credentials to `POST http://localhost:5161/api/auth/login`. Use the returned `accessToken` as `Authorization: Bearer <token>` for protected endpoints. The same requests are available in [`postman/DomainCopilot.postman_collection.json`](../postman/DomainCopilot.postman_collection.json).

## Demo sequence

1. `POST /api/cases` with `{ "syntheticCaseSummary": "Synthetic test scenario only" }` as a `Clinician`.
2. Set a valid role token as `DOMAINCOPILOT_BEARER_TOKEN` and run `./scripts/ingest-synthetic-corpus.ps1` to index the included 30-document synthetic corpus, or upload one public/synthetic PDF or DOCX through `POST /api/documents`.
3. `POST /api/copilot/drafts` with the returned `caseId` and a question.
4. Inspect returned evidence and progress; only a successful safety review returns `AwaitingApproval` and `draftId`.
5. `POST /api/drafts/{draftId}/review` with an approve, reject, or edited-and-approved action as a `Clinician`.

For a local-only model run set `AI_PRIMARY_PROVIDER=ollama` in `.env`, keep `EMBEDDING_PROVIDER=ollama`, pre-pull both models, and then disconnect internet access. A synthetic draft was exercised through all three local Qwen agents and reached `AwaitingApproval`; the test kept internet available. The complete disconnected run has not yet been exercised. Keep the local signing key available for offline authentication.

## Boundaries

The 30-document / 150-page synthetic corpus is bundled and was indexed into SQL Server LocalDB and SQLite using Ollama Nomic embeddings. The live retrieval baseline is recorded in `docs/EVALUATION.md`; it measures retrieval and lexical support, not generated-answer safety. Local Qwen generated a synthetic draft that reached the clinician approval gate. A disconnected end-to-end run remains unverified. OCR, answer-level groundedness, persistent agent traces, a reusable local identity-provider fixture and production EHR publishing remain open tasks. No real medical data is included. Do not use this MVP for patient care.
