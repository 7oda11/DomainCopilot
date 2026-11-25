# Slide content — grounded healthcare RAG (90 minutes)

## 1. Why grounded answers?
- General language models can sound certain while lacking source support.
- Healthcare claims require provenance and human judgment.
- Today: build a traceable path from document to draft.

## 2. Product boundary
- Search public/synthetic clinical material.
- Draft for clinician review; do not diagnose or prescribe.
- Never use real patient data in this assessment demo.

## 3. Personas and permissions
- Researcher: manage approved references and ask evidence questions.
- Clinician: review, edit, approve or reject drafts.
- The server enforces roles; hiding a button is not security.

## 4. Clean Architecture
- Domain: invariants and lifecycle.
- Application: use cases, ports, orchestration.
- Infrastructure: SQL, vectors, models, document extraction.
- API: HTTP boundary and dependency composition.

## 5. Ingestion pipeline
- Accept PDF and DOCX within limits.
- Extract and normalize text; retain source metadata.
- Chunk, embed and index; avoid re-indexing identical content.

## 6. Provenance
- Keep source name, version, page, section and chunk ID.
- Citations point to retrieved excerpts, not just a document title.
- Check that cited text belongs to the referenced chunk.

## 7. Dense and keyword search
- Keyword search is strong for exact terms and identifiers.
- Dense search helps with synonyms and meaning.
- Each method has distinct misses and failure modes.

## 8. Hybrid retrieval
- Combine lexical and vector rankings with a documented fusion.
- Inspect the actual quoted text, not only a similarity score.
- Evaluate hit rate on a fixed golden set.

## 9. When should the system refuse?
- No relevant evidence or conflicting sources.
- Dose/contraindication is not explicitly documented.
- Refusal is safer than filling gaps from model memory.

## 10. Prompt injection
- User questions and retrieved passages are untrusted input.
- Instruction-like text in a document cannot grant tools or roles.
- Direct bypass phrases have a policy gate; indirect injection needs end-to-end testing.

## 11. Specialized agents
- Researcher extracts source-supported findings.
- Drafter writes concise, cited language for review.
- SafetyChecker audits claims and citations.

## 12. Orchestrator
- Fixed sequence, limits, timeout, retry and cancellation.
- Typed inputs/outputs and a constrained tool catalog.
- The model cannot change the workflow or grant itself a capability.

## 13. Human approval
- A model safety pass does not equal a clinical decision.
- Draft enters `AwaitingApproval`.
- Only Clinician can persist approve/reject/edit-and-approve.

## 14. Degraded providers
- Gemini is the hosted option; Ollama can serve local inference.
- Provider identity and degraded status belong in the run trace.
- Fallback success while online does not prove offline operation.

## 15. Offline proof
- Pre-download chat and embedding models.
- Block outbound network while keeping local dependencies available.
- Complete ingestion, search, all agents and approval; preserve evidence.

## 16. Reading evaluation results
- State numerator, denominator, and what the metric measures.
- Retrieval hit rate is not answer correctness or safety.
- Analyze false acceptance and refusal misses before claiming success.

## 17. Observability and privacy
- Store run/agent, provider, elapsed time, tokens and configured cost estimate.
- Never log API keys, passwords, JWTs or full raw clinical prompts.
- Missing usage or price data stays unknown, not zero.

## 18. Takeaways
- Evidence must support each claim.
- Injection defense is a layered system property.
- Offline claims need an actual disconnected run.
- Human clinician review remains the final decision gate.
