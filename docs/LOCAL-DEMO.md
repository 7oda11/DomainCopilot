# Local demo

## Requirements

- Docker Desktop with the Linux container engine running.
- An OpenID Connect identity provider that issues JWTs with `Clinician` and `Researcher` roles.
- Network access for the first image/model download; Gemini is optional for the local workflow.

## Start

1. Copy `.env.example` to `.env` and replace the sample SQL password and identity-provider authority/audience values.
2. Run `docker compose up --build -d`.
3. While online, run `./scripts/pull-models.ps1` to pull `qwen3:8b` and `nomic-embed-text` into the persistent Ollama volume.
4. The Compose API service applies the SQL migrations on startup. In non-Compose environments, apply them explicitly with the repository's pinned EF tool: `dotnet tool restore`, then `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`.
5. Use the OpenAPI document at `/openapi/v1.json` and pass a JWT with the required role.

## Demo sequence

1. `POST /api/cases` with `{ "syntheticCaseSummary": "Synthetic test scenario only" }` as a `Clinician`.
2. `POST /api/documents` with multipart `File` and `Version`, using a public or synthetic PDF/DOCX guideline.
3. `POST /api/copilot/drafts` with the returned `caseId` and a question.
4. Inspect returned evidence and progress; only a successful safety review returns `AwaitingApproval` and `draftId`.
5. `POST /api/drafts/{draftId}/review` with an approve, reject, or edited-and-approved action as a `Clinician`.

For a local-only model run set `AI_PRIMARY_PROVIDER=ollama` in `.env`, keep `EMBEDDING_PROVIDER=ollama`, pre-pull both models, and then disconnect internet access. The complete disconnected run has not yet been exercised in this environment. The OIDC authority itself must also be reachable or configured with a local identity provider for offline authentication.

## Boundaries

The current source corpus is not bundled: no real medical patient content is included. The assessment floor of 30 documents and 150 pages, OCR, measured retrieval/groundedness/refusal evaluation, persistent agent traces, full local identity-provider fixture, and production EHR publishing remain open tasks. Do not use this MVP for patient care.
