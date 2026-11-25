# System Design

## Purpose and scope

Domain Copilot is an assessment MVP for D0T2: it ingests public or synthetic PDF/DOCX guidance, retrieves cited passages, drafts a short note, performs a safety review, and pauses for a clinician decision. It is not a diagnostic or prescribing product. Real patient data is out of scope.

## Requirements and implementation status (2026-11-24)

| Requirement | Current evidence | Status / gap |
|---|---|---|
| Ingest PDF and DOCX with provenance | `IngestDocumentHandler`, SQL entities, extraction adapters | Implemented; OCR for scanned PDFs absent |
| Hybrid search and citations | `HybridSearchHandler`, SQLite vector index + SQL metadata | Implemented; baseline is a small synthetic corpus |
| Evidence-grounded answer/refusal | Researcher, Drafter, SafetyChecker, citation validator | Partial; refusal baseline is 72%, must improve and measure at answer level |
| Orchestrated agents and restricted tools | typed agents, `AgentToolCatalog`, `RunCopilotWorkflowHandler` | Implemented for the demo; no autonomous agent tool execution |
| Human approval | approval domain lifecycle and API | Implemented; API policy restricts review to Clinician |
| Offline/degraded mode | Gemini chat fallback to Ollama; Ollama embeddings supported | Partial; a disconnected full workflow has not been verified |
| Persistent run history and traces | SQL `CopilotRuns`, `WorkflowTraceEntries`, `GET /api/copilot/runs/{runId}/trace` | Implemented for completed agent stages; failed-stage persistence is limited |
| Live streaming/cancellation | progress events via SSE and linked cancellation token | Partial; this streams workflow progress, not generated tokens |
| Role enforcement | JWT roles and endpoint policies | Implemented: only Clinician can review; public registration grants Researcher only |
| Teaching/submission artifacts | `docs/`, `teaching/` | In progress; status must not be represented as complete until rehearsed |

## Main flow

1. An authenticated user submits a case ID and question.
2. The workflow loads the case and performs hybrid retrieval over approved corpus material.
3. An explicit policy gate rejects requests that directly ask to bypass safety or trigger approval. Retrieved text is labeled untrusted in model prompts.
4. Researcher and Drafter produce independent outputs from the same retrieved evidence.
5. SafetyChecker audits the draft against the source excerpts. Invalid citations or failed review result in refusal.
6. The orchestrator saves a draft in `AwaitingApproval`; no approval is inferred from model output.
7. A Clinician approves, rejects, or edits-and-approves through a separately authorized endpoint.

## Offline/degraded design

The intended disconnected profile selects Ollama for chat and embeddings, SQL Server for relational storage, and the local SQLite vector index. The host must have the Ollama service and the exact chat/embedding models already downloaded. Gemini fallback is only available online. Database migration, ingestion, retrieval, all agent calls, and approval must be tested while network access is disabled; successful provider unit tests alone do not prove T2. See `docs/OFFLINE-VERIFICATION.md`.

## Tradeoffs

- SQLite exact vector scan keeps the demo self-contained; it is linear in corpus size and not a large-corpus design.
- Synthetic corpus and summaries avoid real-PHI exposure but do not establish clinical validity.
- Concurrent Researcher and Drafter calls reduce latency but do not share Researcher findings; SafetyChecker receives both and cross-checks them.
- Progress SSE is simple for the Angular UI; production needs durable events and reconnect semantics.
- Token usage can be absent from a provider response. Estimated cost must remain null unless model pricing is configured; never report a fabricated zero.

## Open release blockers

1. Apply the new trace migration and prove the full workflow with networking disabled on the target machine; capture evidence.
2. Run answer-level golden and end-to-end retrieved-document injection evaluation; investigate every unsafe false acceptance.
3. Configure provider token rates before reporting estimated model cost; values remain null when usage or rates are unavailable.
4. Verify role-denial behavior with API-level tests and use the trusted local Clinician provisioning procedure.
5. Rehearse clean Docker setup, demo, teaching lab, and record both required videos.
