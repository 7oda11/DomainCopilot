# Domain Copilot — Delivery Roadmap

## Delivery rules

- Work on one phase at a time. Stop at the phase gate, explain it, and wait for explicit approval before moving on.
- No Git state changes without the user's explicit authorization. Give branch and command guidance before each phase; never push, merge, rewrite history, delete branches or alter repository settings without authorization.
- Every phase ends with changed-file list, code/decision explanation, actual verification results, remaining issues, proposed Git actions and a review/approval request.
- Time estimates target approximately 40 focused implementation hours. Teaching materials, videos, repository history spread over six days and PR administration may require additional calendar time.
- No feature is called implemented until the relevant verification has actually passed.

## Phase plan

| # | Phase / objective | Assessment coverage | Tasks and expected files/folders | Dependencies | Tests and acceptance criteria | Git deliverable guidance | Estimate / risks |
|---:|---|---|---|---|---|---|---|
| 1 | Requirements and project planning | Variant, scope, assumptions, risks, traceability | `docs/BRD.md`, `docs/ROADMAP.md`, initial architecture and repository boundary plan | None | User reviews variant, stack, scope and decisions; no code feature claims | Branch `docs/project-plan`; commit `docs: define D0T2 scope and delivery roadmap`; PR | 2h; workspace currently has no commit baseline and many unrelated untracked paths |
| 2 | Solution structure and Clean Architecture foundation | Clean Architecture, DI, tests, prompt/config hygiene | `src/DomainCopilot.Domain`, `Application`, `Infrastructure`, `Api`; `tests/`; solution/project files; architecture rules | 1 | Build succeeds; project references flow inward; small architecture test/check documents allowed dependencies | Branch `chore/clean-architecture-foundation`; `chore: establish clean architecture solution`; PR | 2h; target .NET 10 LTS; install its stable SDK first because the machine currently has 10.0.100 RC only |
| 3 | Domain model and errors | Typed domain workflow and safety rules | Dedicated files under `Domain/Documents`, `Evidence`, `Runs`, `Approvals`, `Agents`, `Errors` | 2 | Unit tests for invariants, state transitions, invalid approval and evidence states | Branch `feat/domain-model`; `feat: model evidence and approval workflow`; PR | 2h; avoid modelling unneeded clinical data |
| 4 | Application contracts and use cases | Provider/store independence; repository and Unit of Work contracts | `Application/Abstractions`, `Features/Ingestion`, `Retrieval`, `Workflow`, `Approval`, typed requests/results | 3 | Unit tests with fakes; Application has no web/SDK/Infrastructure references; transaction boundaries documented | Branch `feat/application-use-cases`; `feat: define copilot use case contracts`; PR | 3h; keep abstractions narrow, including requested repositories/UoW |
| 5 | Persistence and migrations | Relational and vector stores | SQL Server EF Core context/configurations/migrations; repository/UoW adapters; vector adapter; ADR for vector storage | 2–4 | Migration applies to disposable DB; repository isolation/transaction tests; vector adapter contract tests | Branch `feat/persistence`; `feat: add sql persistence and vector store adapters`; PR | 3h; SQL Server version/setup and SQLite vector tradeoff need validation |
| 6 | Document ingestion | FR-1, corpus floor, source metadata | Extract/clean/chunk/embed/index stage files; supported format adapters; synthetic/public seed corpus manifest | 4–5 | Unit tests per stage; integration tests for two formats, page/source metadata, idempotency and per-document failure; corpus ≥30 docs/150 pages | Branch `feat/document-ingestion`; `feat: add traceable document ingestion pipeline`; PR | 4h; corpus authoring and page count take time; OCR likely deferred |
| 7 | Hybrid retrieval and citations | FR-2, evidence refusal | Keyword/dense retrievers, fusion, one enhancement, evidence DTOs and chunk-level citations; ADR | 5–6 | Integration retrieval set; citations resolve to exact chunks; low-evidence and conflicting-source refusal tests; baseline metric recorded | Branch `feat/hybrid-retrieval`; `feat: add hybrid search with grounded citations`; PR | 4h; quality depends on corpus, chunking and local embedding model |
| 8 | Hosted/local providers and T2 | Provider abstraction, automatic fallback, offline mode | Gemini adapter reusing reviewed concepts; Ollama chat and embeddings adapters; config/capability metadata; provider ADR | 4, 7 | Provider contract tests; forced hosted failure triggers local provider; local-only query and embedding succeed with internet unavailable after model setup | Branch `feat/offline-provider-fallback`; `feat: add local model fallback for degraded mode`; PR | 5h; hardware capacity, model pulls and Ollama availability are primary risks |
| 9 | Healthcare agents and orchestration | FR-4/5: researcher, safety checker, drafter, orchestrator, ≥4 tools | Typed agent contracts, tool schemas/allow-lists, orchestration state machine/pipeline, versioned prompts, run trace and ADR | 3, 7, 8 | Contract tests for schemas and permissions; max iterations, timeout, retry/backoff, plain-RAG degradation, no unsupported clinical inference | Branch `feat/healthcare-agents`; `feat: orchestrate evidence based healthcare agents`; PR | 4h; compact MVP workflow; local models may have weaker structured output |
| 10 | Human approval gate | D0 clinician approval; FR-4/5 | Approval queue/use cases, approve/reject/edit-and-approve, audit persistence, side-effect tool boundary | 9 | Tests prove no consequential tool executes before approval and decisions are auditable | Branch `feat/clinical-approval`; `feat: gate clinical note actions on review`; PR | 2h; ensure edit content remains attributable and revalidated |
| 11 | API, access, streaming and security | FR-6/7/8, OWASP web/LLM controls | API models/endpoints, Identity/auth, two roles, SSE, progress events, cancellation, limits, validation, security headers, `SECURITY.md` | 4–10 | API contract and integration tests; server-side role denial; cancellation reaches work; injection and upload validation tests | Branch `feat/secure-copilot-api`; `feat: expose secured streaming copilot workflow`; PR | 3h; reduce UI scope before weakening security or cancellation behavior |
| 12 | Evaluation and observability | FR-3/9, ≥3 injection cases, honest baseline | Golden/adversarial dataset, runnable harness, hit-rate/groundedness/refusal metrics, correlation and token/cost tracking, `EVALUATION.md` | 6–11 | ≥25 cases, ≥5 adversarial and ≥3 prompt injection; actual repeatable baseline and failure analysis; inspectable run trace and health/readiness | Branch `test/rag-evaluation`; `test: measure retrieval grounding and refusal behavior`; PR | 3h; evaluator labels and groundedness criteria require careful manual review |
| 13 | Packaging, documentation, teaching and submission prep | Docker, env template, ≥4 ADRs, required documents and teaching | Compose, seed/ingest command, `.env.example`, README/demo path, SDD, architecture diagrams, ADRs, agentic workflow/use log, teaching slides/lab/map/mistakes, checklist | 1–12 | Fresh setup and seed verified; docs match actual behavior; deferred gaps listed; videos/publication remain user-produced | Branch `docs/submission-pack`; `docs: complete architecture teaching and run guides`; PR | 3h implementation docs, additional calendar effort for 15–25 slides, videos, CI/PR cadence and fresh-machine check |

## Initial solution layout

```text
DomainCopilot/
  src/
    DomainCopilot.Domain/
      Documents/ Evidence/ Runs/ Approvals/ Agents/ Errors/
    DomainCopilot.Application/
      Abstractions/ Features/Ingestion/ Features/Retrieval/
      Features/Workflow/ Features/Approval/ Validation/
    DomainCopilot.Infrastructure/
      Persistence/ Repositories/ UnitOfWork/ VectorStore/
      Documents/ AI/Providers/ AI/Embeddings/ Observability/
    DomainCopilot.Api/
      Endpoints/ Authentication/ Authorization/ Middleware/ Configuration/
  tests/
    DomainCopilot.Domain.Tests/
    DomainCopilot.Application.Tests/
    DomainCopilot.Infrastructure.IntegrationTests/
    DomainCopilot.Api.ContractTests/
    DomainCopilot.Evaluation/
  docs/
    BRD.md ROADMAP.md SYSTEM-DESIGN.md ARCHITECTURE.md SECURITY.md
    EVALUATION.md AGENTIC-WORKFLOW.md AI-USAGE-LOG.md adr/
  teaching/
  data/synthetic/
  docker-compose.yml
  .env.example
```

The exact project and namespace names can be adjusted once the solution is created. One type per file remains the default. Domain and Application do not depend on Infrastructure, ASP.NET Core, provider SDKs or vector-store SDKs. API request/response types remain separate from Application contracts and Domain entities.

## Git and review strategy

- The workspace root is currently an unborn `master` branch with many unrelated untracked directories. Do not stage its root indiscriminately.
- Before the first Git mutation, create/confirm a repository boundary containing only `DomainCopilot` (or obtain permission to initialize a repository inside it). This avoids including training material and unrelated projects.
- Use short-lived branches and a PR for each coherent phase or split an oversized phase into honest atomic PRs. Every PR should include purpose, test evidence, limitations and a self-review checklist; link it to a real issue.
- Candidate PRs are listed by phase. Thirty meaningful commits over at least six real work days and eight PRs are process goals from the rubric, not a reason to create artificial changes. Keep actual dates; never fabricate history or claim uncreated PRs.
- Protect `main`, CI, dependency/secret scans and release tags only after the GitHub repository exists and the user authorizes those external changes.

## Scope cuts if time runs short

Defer UI richness first, then optional retrieval enhancement, then nonessential agent breadth (retain all three required agents plus orchestrator), then corpus breadth only as permitted by the 30-document floor. Keep evaluation, security, core offline proof, documentation and teaching artifacts visible. Record every cut, interim mitigation and effort to close it in `SYSTEM-DESIGN.md`.

