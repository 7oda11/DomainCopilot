# Offline / degraded-mode verification

## Current result

**Not yet verified end to end.** Gemini-to-Ollama chat fallback and Ollama embedding adapters exist. The existing local workflow smoke test ran while internet access was available, so it is not evidence of T2 offline compliance. Do not mark the requirement passed until the checklist below has been run on the assessment machine. Configure `AI:PrimaryProvider=ollama` in this profile so a local-only run cannot use Gemini as its primary provider.

## Preparation (while online)

1. Install/start Ollama and pull the exact models configured in `src/DomainCopilot.Api/appsettings.json` (chat and embedding). Record model names and versions with `ollama list`.
2. Configure `AI:PrimaryProvider=ollama`, `AI:FallbackProvider=ollama`, and `Embedding:Provider=ollama` using user secrets/environment variables.
3. Start SQL Server and apply migrations. Ingest the bundled synthetic corpus and verify local SQLite vector-index files are present.
4. Stop the API. Confirm the Ollama chat and embedding models load locally before disconnecting.

## Disconnected run

1. Disable Wi-Fi/Ethernet or use an OS firewall rule that blocks outbound network access for the API process. Keep localhost available for SQL Server, Ollama and the browser.
2. Restart Ollama and SQL Server, then start the API with outbound access still blocked.
3. Register/login with a local account; create a synthetic case; run retrieval and the full Researcher/Drafter/SafetyChecker workflow; inspect citations and the AwaitingApproval state; approve it with a Clinician account.
4. Upload a small synthetic PDF and DOCX if the disconnected requirement covers ingestion, then confirm query retrieval from them.
5. Save timestamped logs, screenshots, configuration (redacting secrets), model list, run ID, provider IDs and approval result. Verify no DNS/HTTP request leaves localhost.
6. Re-enable network and record the exact test date, machine, commands, outcomes and failures in `docs/EVALUATION.md`.

## Expected failure modes

- Missing Ollama model: stop with provider-unavailable; never silently call Gemini in a forced-local profile.
- Embedding provider/model mismatch: rebuild the derived index with the same local model used for queries.
- SQL Server/Ollama unavailable: health/readiness must show dependency failure; do not claim the workflow is offline-ready.
- Model latency: local inference may be slower; capture stage timings and use the UI progress stream.

## Evidence template

| Field | Value |
|---|---|
| Date and machine | Pending |
| Network disabled method | Pending |
| Ollama chat / embedding models | Pending |
| SQL Server and API startup | Pending |
| Full workflow run ID / provider IDs | Pending |
| Clinician approval outcome | Pending |
| External network observed | Pending |
| Artifacts | Pending |
