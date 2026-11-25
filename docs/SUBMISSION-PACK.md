# D0T2 Submission Pack Guide

**Project:** HealthCareCopilot / Domain Copilot
**Variant:** D0T2 — Healthcare with Offline / Degraded Mode
**Snapshot date:** 25 November 2026

This guide maps the assessment checklist to the repository, states what has been demonstrated, and identifies what still needs evidence. “Implemented” means the capability or document is present; it does not imply production readiness or a passing acceptance test.

## 1. Repository and 15-minute start

The source repository is `https://github.com/7oda11/DomainCopilot`. Before submission, confirm that GitHub shows it as **Public** and keep it public for the required 30-day period. Keep source, agent configuration, docs, migrations, synthetic data, Docker setup, scripts, tests, and teaching materials in the repository. Do not commit `.env`, model weights, local vector indexes, credentials, JWTs, or real patient data.

Docker quick start:

1. Install Docker Desktop with its Linux engine enabled, and install Git.
2. Clone the repository and open its root folder.
3. Copy `.env.example` to `.env`; replace the SQL Server password and set a unique signing key with at least 32 bytes. Add `GEMINI_API_KEY` only if using Gemini.
4. Run `docker compose up --build -d`.
5. While online, run `./scripts/pull-models.ps1` in PowerShell to download `qwen3:8b` and `nomic-embed-text` into the persistent Ollama volume. Choose Ollama as the primary provider for the fully local demo.
6. Open Swagger at `http://localhost:8080/swagger` (Compose) or `http://localhost:5161/swagger` (local .NET launch profile). Register and log in; public registration grants the Researcher role. Provision a Clinician only through the documented trusted local demo procedure.
7. Follow the 5-Minute Demo Path below. For indexed content, use the fictional corpus only.

### Configuration variables

| Variable | Purpose | Required / default |
|---|---|---|
| `MSSQL_SA_PASSWORD` | SQL Server container password | Required in Compose; set a strong local-only value |
| `AUTHENTICATION_ISSUER` | JWT issuer | `DomainCopilot` |
| `AUTHENTICATION_AUDIENCE` | JWT audience | `domain-copilot-api` |
| `AUTHENTICATION_SIGNING_KEY` | JWT signing secret | Required; at least 32 bytes; keep secret |
| `GEMINI_API_KEY` | Hosted Gemini credential | Optional for Ollama-only use; obtain from Google AI Studio |
| `AI_PRIMARY_PROVIDER` | Chat provider (`gemini` or `ollama`) | `gemini` in sample Compose settings |
| `EMBEDDING_PROVIDER` | Embedding provider | `ollama` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection | Supplied internally by Compose; configure for non-Compose hosting |
| `AI__FallbackProvider` | Chat fallback provider | Compose fixes this to `ollama` |
| `AI__Ollama__BaseUrl` | Ollama service base URL | `http://ollama:11434` in Compose |
| `Embedding__Ollama__Endpoint` | Ollama embedding endpoint | `http://ollama:11434/api/embed` in Compose |
| `VectorStore__SqlitePath` | Local derived vector index path | `/app/App_Data/domaincopilot-vectors.db` in Compose |
| `Database__ApplyMigrationsOnStartup` | Apply EF migrations on startup | `true` in Compose |
| `ASPNETCORE_ENVIRONMENT` | API environment | `Development` in Compose |

For local .NET runs, use .NET SDK 10.0.404 as pinned in `global.json`, restore the local EF tool with `dotnet tool restore`, and apply migrations with `dotnet ef database update --project src/DomainCopilot.Infrastructure --startup-project src/DomainCopilot.Api`. The `.env.example`, `docker-compose.yml`, `Dockerfile`, `docs/LOCAL-DEMO.md`, and `docs/PROVIDERS.md` are the source of truth; confirm the variables if configuration changes.

### 5-Minute Demo Path

1. **Sign in:** register a Researcher and log in; show the bearer-token workflow.
2. **Ingest:** upload a fictional PDF or DOCX, or index the 30-document synthetic corpus; show source and chunk provenance.
3. **Ask with evidence:** search a supported question and inspect the cited source passages.
4. **Create a case and draft:** submit a synthetic case and question; show live workflow progress and the evidence-backed draft.
5. **Inspect the trace:** open the run trace and point out agent stage, provider, elapsed time and available token counts.
6. **Enforce the approval gate:** try review with a Researcher (expected 403), then approve or reject with a Clinician and show the persisted decision.
7. **Show degraded mode honestly:** select Ollama and describe the recorded process-level egress-isolated run; state that a fully disconnected host run remains unverified.

## 2. Business Requirements Document

The detailed BRD is [`BRD.md`](BRD.md). It covers context, personas and server-enforced permissions; measurable objectives; 15 uniquely identified requirements and acceptance evidence; non-functional and safety requirements; explicit out-of-scope items; assumptions; and risks. The matrix below links each BR requirement to current evidence and the remaining proof. Statuses are intentionally candid.

| ID | Requirement and acceptance focus | Current status | Evidence / remaining acceptance |
|---|---|---|---|
| BR-01 | Ingest at least two formats through extraction, cleanup, chunking, embedding and indexing; show per-document outcome and idempotency | Implemented; fresh-environment acceptance pending | `docs/INGESTION.md`, PDF/DOCX ingestion handlers and tests; repeat duplicate-ingest check |
| BR-02 | Preserve source, section, page, version and chunk provenance through retrieval/citations | Implemented | `docs/ARCHITECTURE.md`, source/document entities and citation construction; verify a citation resolves to its persisted chunk |
| BR-03 | Dense + keyword retrieval with documented fusion | Implemented | `docs/RETRIEVAL.md`, `docs/adr/ADR-004-hybrid-retrieval.md`, retrieval baseline |
| BR-04 | Justify and compare one retrieval enhancement against baseline | Partial | RRF and ADR exist; a controlled before/after comparison is still needed |
| BR-05 | Ground claims in evidence and refuse when insufficient; evaluate answer-level claims | Partial | Citation validation and retrieval refusal exist; current 72% refusal agreement is retrieval-only, not answer safety |
| BR-06 | Typed researcher/drafter/safety workflow, orchestrator, bounded execution | Implemented for fixed workflow | `docs/AGENTIC-WORKFLOW.md`, workflow handler and ADR-006; autonomous tool planning is deliberately out of scope |
| BR-07 | At least four restricted tools, including a side-effecting tool that waits for approval | Implemented in fixed pipeline; acceptance tests pending | Search, read-case, validate-citations, and queue-draft tools; queueing only creates `AwaitingApproval` |
| BR-08 | Clinician approve, reject, or edit-and-approve with actor/time/content persisted | Implemented; all outcomes need repeatable API tests | Review API and approval records; capture tests for each outcome |
| BR-09 | Iteration cap, timeout, retry/backoff, and plain-RAG graceful fallback | Partial | Limits/fallback paths exist in provider/workflow; deterministic tests covering each path are still required |
| BR-10 | Stream tokens and progress; cancellation stops server work | Partial | SSE streams workflow progress and cancellation is propagated; generated-token streaming is not implemented |
| BR-11 | Authentication, two meaningful server-enforced roles, persistent session/run history | Implemented; clean test evidence pending | JWT policies, Researcher 403 on review, Clinician approval, workspace/run history |
| BR-12 | Correlation, trace, provider/degraded state, tokens/cost, health/readiness | Partial | Completed-agent traces persist timing/provider/token counts; costs can be unknown, failed/retrieval traces and full API-level coverage remain |
| BR-13 | Replaceable hosted/local provider contract with completion, streaming, tool calling and embeddings | Partial | Gemini/Ollama chat and local embeddings exist; capability parity/contract tests for every capability remain |
| BR-14 | Repeatable Docker setup and seeded demo path on a clean environment | Partial | Docker Compose and synthetic corpus exist; clean-machine rehearsal and seeded-account path remain |
| BR-15 | Two working implementations behind provider abstraction for all required capabilities | Partial | Chat and embedding paths exist, while full streaming/tool-call contract coverage remains unverified |

## 3. System Design Document

The main design document is [`SYSTEM-DESIGN.md`](SYSTEM-DESIGN.md). It separates **Part A — unconstrained target architecture** from **Part B — implemented MVP**. Part A should describe the managed API gateway and distributed rate limits, secret manager, broker, autoscaling, cache, managed vector service, observability stack, CI/CD environments, disaster recovery/backups, and cost drivers at scale. Part B must retain a gap table with: target component, implementation status, reason deferred, interim mitigation, and estimated effort/cost to close. Do not claim a paid tier is the only reason: explain the operational risk and accepted limitation.

The implemented choices and tradeoffs are captured in the design and ADRs. Current known expedients to call out:

- SQLite exact cosine scan is simple and offline-friendly, but linear in corpus size and single-node; SQL Server remains canonical for relational data.
- In-process rate limiting is 60 requests/minute per process; it does not coordinate across replicas, leaving abuse-driven token spend as an operational risk.
- SSE sends stage progress rather than generated tokens and lacks durable reconnect semantics.
- Fixed orchestration is testable and bounded, but is not autonomous model-selected tool execution.
- Hosted model usage/price data can be missing; unknown cloud cost must stay null rather than be reported as zero.
- Four current ADRs are `ADR-003` through `ADR-006`; confirm they satisfy the assignment's “at least four” interpretation and add earlier consequential decisions if necessary.

For a scale cost model, show the formula and units rather than fabricate a monthly quote: `monthly cost = API compute + SQL compute/storage/backup + vector service/storage + queue/cache/observability + (input tokens × input rate) + (output tokens × output rate)`. Label traffic assumptions (requests/day, average retrieved chunks, tokens/request, retention, replicas) and obtain current vendor rates before entering currency figures. Local demo model compute is hardware electricity/capacity, not a free managed service.

## 4. Architecture documentation

[`ARCHITECTURE.md`](ARCHITECTURE.md) contains editable Mermaid source for C4 Levels 1–3, the streamed agent workflow through the human approval gate, trust-boundary/data-flow notes, the ER diagram, and layer-dependency rules. The repository has four ADRs under [`adr/`](adr/): vector store, hybrid retrieval, provider fallback, and bounded orchestration/approval. Before presenting, render all Mermaid diagrams in the target Markdown viewer and verify the described implementation matches current code.

## 5. README and reproducibility

[`README.md`](../README.md) and [`LOCAL-DEMO.md`](LOCAL-DEMO.md) cover Docker, local configuration, Swagger, database migrations, models, ingestion, and the API workflow. Keep the README usable by someone with Docker but no other project setup. Ensure it includes: 15-minute quick start, every environment variable, how to get a Gemini key (Google AI Studio), local Ollama setup without a key, migrations, tests and evaluation commands, demo identities/provisioning, troubleshooting, and the numbered 5-Minute Demo Path above. No demo password or JWT should be committed. Seeded Clinician accounts are not currently safe to claim; public registration is Researcher-only and the local Clinician procedure is in `ROLE-PROVISIONING.md`.

## 6. Security and evaluation reports

- [`SECURITY.md`](SECURITY.md) describes authentication, role boundaries, file limits, redaction limits, logging, provider handling, rate limiting and deployment controls. It explicitly says this is not clinically validated and injection resistance is not fully proven.
- [`EVALUATION.md`](EVALUATION.md) reports 25 retrieval cases: source hit rate **100% (18/18 answerable)**, lexical groundedness proxy **100% (18/18 labeled claims)**, and refusal-flag agreement **72% (18/25)**. The seven refusal misses are H017, H018, H021, H022, H023, H024 and H025.
- These values measure retrieval and exact term presence in cited passages. They do **not** establish semantic entailment, generated-answer accuracy, clinical safety, or end-to-end resistance to malicious instructions in documents.
- The offline workflow report is [`reports/offline-workflow-2026-11-25.md`](reports/offline-workflow-2026-11-25.md). Ollama chat + embeddings completed RAG, all three agents, and Clinician approval with the API's outbound HTTP path isolated. Windows prevented a stable host-wide network disconnect; describe T2 offline as **partial/unverified** until the workflow succeeds with the machine genuinely disconnected.

## 7. Teaching pack and presentations

The teaching materials are in [`../teaching/`](../teaching/): `SESSION.md` (90-minute plan), `LAB.md`, `CHALLENGES.md`, `ASSESSMENT-MAP.md`, `MISCONCEPTIONS.md`, and `SLIDES.md` (18-slide topic outline). The project overview deck and short RAG explainer are in [`../teaching/presentations/`](../teaching/presentations/). For a 90-minute postgraduate session, use the RAG deck with the lab and instructor notes; the project deck is an end-to-end architecture and assessment walkthrough.

Before submission, confirm the teaching pack includes 15–25 teaching slides with timings, a trainee lab with expected outputs and answer key, at least three stretch challenges, outcome-to-assessment mapping, and a one-page list of five predictable misconceptions with corrections. Rehearse the demo and local model download before the session.

## Final submission checklist

- [ ] GitHub repository is public and will remain public for 30 days; all required source/config/docs/migrations/synthetic seed corpus/Docker files are present.
- [ ] Docker quick start succeeds from a fresh clone within 15 minutes, including migrations and model pre-pull.
- [ ] The public Researcher and trusted Clinician workflows are demoed; approval is only possible as Clinician.
- [ ] Tests and evaluation harness run from a clean setup; evaluation numbers include denominators and failure analysis.
- [ ] A truly disconnected full workflow is captured, or the offline limitation is explicitly accepted as a gap.
- [ ] Cost model states traffic assumptions and current rate sources; unknown usage remains unknown.
- [ ] All Mermaid diagrams render, all four ADRs are readable, and Part A/Part B plus the gap table are present in the System Design.
- [ ] Teaching lab, answer key, assessment map, misconception note, presentations and 5-minute demo are rehearsed.
- [ ] No credentials, real patient data, unreviewed output, or misleading claims are included in the public repository or slides.
