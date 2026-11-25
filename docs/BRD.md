# Business Requirements Document

## 1. Purpose

Domain Copilot is an explainable, healthcare-focused assistant for searching a controlled collection of public or synthetic clinical guidance and preparing a draft clinical note for clinician review. It is an instructor assessment project, not a clinical decision system. It must cite the exact source chunks supporting its output and refuse when evidence is missing.

## 2. Variant and boundaries

- Assigned variant: **D0T2 — Healthcare with Offline / Degraded Mode**.
- The assignment states this variant directly; no National ID derivation is assumed.
- Data: public guidance or synthetic documents only. Real patient data is prohibited.
- A clinician is the final decision-maker and must approve any consequential note.
- The MVP supports a documented subset of the full brief; deferred scope must remain visible in `SYSTEM-DESIGN.md` and this traceability matrix.

## 3. Context and personas

| Persona | Need | Permission boundary |
|---|---|---|
| Clinician | Search evidence, review a draft, approve/reject/edit it | Can review and decide on assigned drafts; cannot change system configuration |
| Researcher | Ingest approved public/synthetic guidance and ask evidence questions | Can manage documents if granted that role; cannot approve clinical notes by default |
| Administrator | Manage accounts, roles and operational configuration | Administrative powers are server-side and audited |
| Instructor / evaluator | Inspect run traces, evidence links, tests and evaluation results | Uses seeded demo data; no access to real personal health information |

Role assignments must be genuinely different and enforced by the server. UI visibility alone is not authorization.

## 4. Objectives and success measures

1. Ingest at least 30 public or synthetic documents totaling at least 150 pages across at least two formats.
2. Preserve source, section, page, version and chunk identifiers through ingestion, retrieval and citations.
3. Provide hybrid keyword and dense retrieval with a documented fusion method and at least one measured enhancement.
4. Refuse unsupported clinical questions, especially requests for dosage or contraindications not supported by cited evidence.
5. Complete a typed D0 workflow with Guideline Researcher, Safety Checker, Documentation Drafter and Orchestrator; clinician approval is required before side effects.
6. Demonstrate the hosted-to-local model fallback and a local-only run with network access unavailable after required models are installed.
7. Make a run inspectable by ID, including step, agent, tool, evidence, provider/degraded status, timing and token/cost information where available.
8. Run an evaluation harness over at least 25 golden cases, including at least five adversarial cases and three prompt-injection cases, and publish actual results with failure analysis.
9. Provide a repeatable local setup, security report, architecture documents, teaching pack and an honest submission checklist.

Evaluation thresholds will be declared with the evaluation set before measuring. They must not be changed after observing results to make a weak baseline appear successful.

## 5. Functional requirements

| ID | Requirement | Acceptance evidence |
|---|---|---|
| BR-01 | Ingest two or more formats through extract, clean, chunk, embed and index stages | Stage-level tests; status and failure reason per document; idempotent re-ingestion check |
| BR-02 | Preserve document and chunk provenance, including source, section, page and version | Persisted metadata and citations resolving to exact chunks |
| BR-03 | Retrieve using dense and keyword search and documented fusion | Integration test against a seeded corpus; reproducible retrieval evaluation |
| BR-04 | Improve retrieval with one justified enhancement | Enhancement documented in an ADR and compared against a baseline |
| BR-05 | Answer only from retrieved evidence and refuse insufficient evidence | Golden refusal cases; citations attached to claims or answer segments |
| BR-06 | Run the D0 clinical workflow with three specialized agents and an orchestrator | Typed input/output contracts, restricted tool allow-lists and explicit termination conditions |
| BR-07 | Provide at least four tools, including one side-effecting tool | Tool schema/contract tests; side-effecting tool is unreachable until approval |
| BR-08 | Require clinician approve, reject or edit-and-approve action for the clinical note | Persisted decision, actor, timestamp and edited content; tests for all outcomes |
| BR-09 | Enforce iteration cap, per-step timeout, retry/backoff and graceful fallback to plain RAG | Deterministic tests for each limit and fallback path |
| BR-10 | Stream token output and live progress; cancellation stops server work | API integration check for event order and cancellation token propagation |
| BR-11 | Authenticate users, enforce at least two meaningful roles, and persist session/run history | Authorization tests for allowed and denied actions |
| BR-12 | Report correlation ID, run trace, active provider/degraded state, token/cost data and health/readiness | API contract tests and trace lookup by run ID |
| BR-13 | Operate with hosted and local model providers through replaceable contracts | Provider contract tests; automatic fallback test; local-only test with internet unavailable |
| BR-14 | Provide a runnable local environment and seeded demo path | Fresh-environment Docker Compose and seed/ingest instructions |
| BR-15 | Expose completion, streaming, tool calling and embeddings through the required provider abstraction with at least two working implementations | Contract tests cover all four capabilities; provider capability limits are documented |

## 6. Non-functional requirements and safety rules

- Clean Architecture: Domain and Application have no dependency on web frameworks, LLM SDKs, vector-store SDKs or Infrastructure implementations.
- SQL Server is the relational database. Repository and Unit of Work abstractions are included as requested, with narrowly scoped responsibilities and separate files for each type.
- The vector store is behind an Application-owned abstraction. The planning default is a local SQLite-backed adapter, subject to the vector-store ADR and environment verification; SQL Server remains the canonical relational store.
- Gemini is the initial hosted provider. Ollama is the recommended local provider. Local embeddings are required for offline retrieval; a chat-only local fallback does not satisfy T2.
- The assessment asks for one provider abstraction covering completion, streaming, tool calling and embeddings. Its shape and capability reporting will be documented before implementation so the common boundary stays understandable where providers differ.
- No model output may directly execute SQL, shell commands, file paths or consequential actions. Tool arguments are validated against typed schemas.
- Prompt injection in user input and retrieved documents is treated as untrusted content; retrieval text cannot override system policy or grant tools.
- Do not infer dosage, contraindications or other clinical facts. Missing, conflicting or weak evidence requires a refusal or escalation.
- Secrets are external configuration only. Logs exclude credentials, full patient-like records and unnecessary raw document content.
- Every meaningful class/interface/enum/DTO/request/response has a dedicated file. API models remain separate from Application and Domain types.

## 7. Out of scope for the MVP

- Diagnosis, treatment recommendation, prescribing, dosage calculation or autonomous clinical action.
- Real patient data, production clinical deployment, regulatory certification or claims of clinical validation.
- OCR/image interpretation unless the schedule permits it; scanned PDFs will be identified and reported as unsupported if OCR is deferred.
- Multi-region/horizontally scaled production infrastructure, managed secrets service, enterprise SSO and managed vector service.
- A polished consumer UI. The MVP surface is functional and limited to ingestion, asking with citations, running the workflow, acting on approval and viewing a trace.
- Guaranteed parity between hosted and local model quality. Capability and quality differences will be measured and documented.

## 8. Assumptions and risks

| ID | Assumption / risk | Mitigation or decision needed |
|---|---|---|
| A-01 | The assessment assigns D0T2 directly | Record the assigned variant; do not infer from an ID |
| A-02 | .NET is preferred because an existing .NET codebase and working Gemini patterns are available | Create a new solution; reuse only reviewed patterns, not the old study-assistant domain model |
| A-03 | SQL Server is the requested relational database | Verify installed version and local connection approach before persistence implementation |
| A-04 | Ollama is available for local inference | Check machine RAM/disk and model size; download models before offline verification |
| A-05 | Local embeddings must also work offline | Select and pin a compatible local embedding model with the local provider ADR |
| A-06 | Hosted and local models may differ in structured output and tool support | Define provider capabilities; validate outputs in Application; record degraded limitations |
| A-07 | 40 focused hours is constrained relative to all deliverables | Prioritize trustworthy RAG, safety, approval, offline proof, evaluation, documentation and teaching; record cuts explicitly |
| A-08 | Existing workspace has no commits and many unrelated untracked directories | Create a separate project repository boundary before the first Git mutation; do not stage workspace root wholesale |
| A-09 | Assessment requires 30 meaningful commits across six days and eight PRs | Build a realistic issue/PR plan across actual work days; never fabricate commits, dates or PRs |
| A-10 | Medical guidance versions can conflict or become stale | Preserve version/effective-date metadata and surface conflicting sources for human review |

## 9. Architecture decisions to record

The architecture choices are recorded in `docs/adr/ADR-003-vector-store.md`, `ADR-004-hybrid-retrieval.md`, `ADR-005-provider-fallback.md`, and `ADR-006-orchestration-and-human-approval.md`. Earlier chunks/orchestration ADR coverage remains a gap if the instructor interprets each named decision as requiring its own separate ADR.

## 10. Requirements traceability

This is a current-state traceability snapshot. “Implemented” describes code/document presence; it does not substitute for a measured acceptance run.

| Requirement | Current status | Evidence / remaining acceptance |
|---|---|---|
| D0 healthcare workflow and clinician decision | Implemented, synthetic-only; end-to-end acceptance still needed | Domain workflow, agent handlers, approval API; exercise approve/reject/edit paths |
| T2 offline/degraded behavior | Partial; disconnected end-to-end is **unverified** | `docs/OFFLINE-VERIFICATION.md`; run with network disabled and record artifacts |
| 30 documents / 150 pages | Previously verified locally on synthetic corpus | `data/synthetic/corpus-manifest.json`; repeat on submission machine if required |
| PDF/DOCX ingestion, provenance and idempotency | Implemented; repeatable clean-environment proof needed | Infrastructure tests and `docs/INGESTION.md` |
| Hybrid retrieval and citation validation | Implemented; baseline corpus/evaluation is limited | `docs/RETRIEVAL.md`, `docs/EVALUATION.md` |
| Refusal and injection resistance | Partial; refusal agreement is 72%; gate unit tests cover explicit direct bypass requests only | End-to-end answer-level and retrieved-document injection evaluation remains |
| Typed agents, tools and bounded orchestration | Implemented for fixed workflow | `docs/AGENTIC-WORKFLOW.md`; no autonomous model-selected tool execution |
| Clinician-only approval | Implemented in API policy; API-level 403 test still required | `ApprovalController`, `Program.cs`, `docs/ROLE-PROVISIONING.md` |
| User roles and authentication | Implemented; public registration is Researcher-only | JWT role policies; verify both roles via API test |
| Streaming, progress, cancellation | Progress SSE implemented; generated token streaming not implemented | `POST /api/copilot/drafts/stream`; cancellation test and token-stream capability remain |
| Persistent run and agent trace | Implemented for completed AI agent stages; migration must be applied | `WorkflowTraceEntries`, `GET /api/copilot/runs/{runId}/trace` |
| Token/cost observability | Token counts stored when provider reports them; estimated cost requires configured rate values | `docs/PROVIDERS.md`; failures/retrieval are not yet in trace table |
| Clean Architecture, SQL, vector adapter, UoW/repository | Implemented in solution; clean restore/build was blocked in current environment | Architecture dependency tests, SQL Server migrations, SQLite index |
| At least 4 ADRs | 4 decisions now recorded | `docs/adr/ADR-003` through `ADR-006` |
| System design, architecture and AI usage log | Added | `docs/SYSTEM-DESIGN.md`, `docs/ARCHITECTURE.md`, `docs/AI-USAGE-LOG.md` |
| Teaching pack | Added outlines/materials; instructor rehearsal and slide deck/video files still required | `teaching/` |
| Security/evaluation reports | Present; answer-level safety and full injection results remain missing | `docs/SECURITY.md`, `docs/EVALUATION.md` |
| Git process, Docker fresh setup, CI, videos and submission email | Not completed by these code changes | Must be completed and evidenced before submission; do not invent history or test evidence |

