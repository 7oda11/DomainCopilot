# Security notes

## Authentication and access

- The API refuses to start unless `Authentication:Authority` and `Authentication:Audience` are configured.
- JWT bearer validation uses the configured identity provider. All endpoints require an authenticated user by default. The `Clinician` role is required to create synthetic clinical cases and make draft approval decisions; document and search endpoints are for authenticated users.
- `ReviewerId` comes from the validated identity claim, never from the review request body.
- Review decisions are persisted with action, reviewer, timestamp, comment and edited content. The run state machine blocks repeated or out-of-order decisions.

## Input, privacy and provider safety

- Uploads accept PDF and DOCX only, capped at 25 MiB; extracted input is capped at 500 pages and 5 million characters. DOCX XML parsing prohibits DTDs and external entity resolution.
- Case-summary ingestion redacts common email, Egyptian national ID and Egyptian mobile-number patterns. This is not a general-purpose PII detector and does not replace synthetic-data-only handling.
- Prompt and completion bodies are not logged. Logs use structured event metadata and correlation IDs. The text of a public guideline can contain indirect prompt injection, so agents are instructed to treat excerpts as data and have no direct external tools.
- Every provider has an output-token cap and timeout; API rate limiting is 60 requests per minute per process. Production deployments need an identity-provider-backed distributed limiter and gateway upload limits.
- The API returns only citations created from persisted chunk text; the draft flow refuses without evidence and blocks safety-review failures. A model-based safety review is a defense layer, not clinical validation.

## Dependencies and deployment

NuGet audit runs during restore and in CI. The SQL driver dependency is pinned to the patched Pkcs line and the native SQLite bundle is pinned to 2.1.14 because the SQL/SQLite dependency graph selected earlier vulnerable transitive versions. Dependabot checks NuGet and GitHub Actions updates weekly. Keep `.env`, local SQLite indexes, model files and connection credentials out of Git.

Configure HTTPS, identity roles, backups, disk encryption, SQL access controls, retention, secret scanning and container scanning in the deployment environment before using real patient information. The assessment demo must use synthetic or public documents only.
