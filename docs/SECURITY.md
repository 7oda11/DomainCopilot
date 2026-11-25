# Security notes

## Authentication and access

- The API refuses to start unless `Authentication:Issuer`, `Authentication:Audience`, and a signing key of at least 32 bytes are configured. Store signing keys in User Secrets locally and a deployment secret manager in production.
- `POST /api/auth/register` creates a `Researcher` account; clients cannot choose a role. Passwords are hashed with ASP.NET Identity's versioned password hasher. `POST /api/auth/login` issues a one-hour signed JWT. Invalid credentials use a generic response. Duplicate normalized email addresses are rejected by an SQL unique index.
- All other endpoints require a valid bearer JWT by default. Only `Clinician` can approve/reject/edit-and-approve drafts. `Researcher` can upload guidance; both roles can search, create synthetic cases and request drafts. Public registration never grants the privileged `Clinician` role; local provisioning steps are in `ROLE-PROVISIONING.md` and production still needs a trusted audited role-management flow.
- JWT signing uses a symmetric secret. Production deployments must provision a high-entropy secret, rotate it deliberately, use HTTPS, and avoid sharing the local development signing key.
- `ReviewerId` comes from the validated identity claim, never from the review request body.
- Review decisions are persisted with action, reviewer, timestamp, comment and edited content. The run state machine blocks repeated or out-of-order decisions.

## Input, privacy and provider safety

- Uploads accept PDF and DOCX only, capped at 25 MiB; extracted input is capped at 500 pages and 5 million characters. DOCX XML parsing prohibits DTDs and external entity resolution.
- Case-summary ingestion redacts common email, Egyptian national ID and Egyptian mobile-number patterns. This is not a general-purpose PII detector and does not replace synthetic-data-only handling.
- Prompt and completion bodies are not logged. Completed agent traces store provider/stage, elapsed time, token counts and optional configured Gemini cost, but not prompt or completion text. Logs use structured event metadata and correlation IDs. The text of a public guideline can contain indirect prompt injection, so agents are instructed to treat excerpts as data and have no direct external tools; this defense is not yet validated end to end.
- Every provider has an output-token cap and timeout; API rate limiting is 60 requests per minute per process. Production deployments need an identity-provider-backed distributed limiter and gateway upload limits.
- The API returns only citations created from persisted chunk text; the draft flow refuses without evidence and blocks safety-review failures. A model-based safety review is a defense layer, not clinical validation.

## Dependencies and deployment

NuGet audit runs during restore and in CI. The SQL driver dependency is pinned to the patched Pkcs line and the native SQLite bundle is pinned to 2.1.14 because the SQL/SQLite dependency graph selected earlier vulnerable transitive versions. Dependabot checks NuGet and GitHub Actions updates weekly. Keep `.env`, local SQLite indexes, model files and connection credentials out of Git.

Configure HTTPS, identity roles, backups, disk encryption, SQL access controls, retention, secret scanning and container scanning in the deployment environment before using real patient information. The assessment demo must use synthetic or public documents only.
