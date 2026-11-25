# Lab: follow one grounded draft (45 minutes)

## Setup

Follow `docs/LOCAL-DEMO.md`. Use the bundled synthetic documents and demo accounts only. Confirm the API and SQL Server are healthy and login succeeds.

## Tasks and expected observations

1. Upload/index one supported synthetic PDF or DOCX. Expected: document ID and nonzero chunk count.
2. Search for a fact stated in the document. Expected: cited source name, version, page/section where available, and quoted text.
3. Ask for a dosage not present in the source. Expected: refusal or escalation; no unsupported dose in a draft.
4. Start a draft for a supported question. Expected: progress for evidence retrieval, Researcher, Drafter, SafetyChecker and save; terminal status `AwaitingApproval`.
5. Attempt review as Researcher. Expected: HTTP 403. Review as Clinician. Expected: decision saved with actor and timestamp.
6. Inspect the run and discuss the difference between retrieval hit rate and answer safety.

## Troubleshooting

- Empty evidence: check corpus ingestion and embedding provider/model consistency.
- Gemini unavailable: confirm Ollama is running and configured chat model is already pulled.
- Slow local stage: inspect progress and model hardware requirements; do not disable safety review.
- 403: use the correct role account; changing UI state does not grant a role.

## Lab evidence checklist

Capture document ID, cited search output, one refusal case, run status, progress stages, 403 denial for Researcher approval, and successful Clinician decision. Remove credentials and tokens from screenshots.
