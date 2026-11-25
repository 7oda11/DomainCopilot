# Offline / degraded-mode verification

## Current result

**Partially verified; host-wide disconnection is still pending.** On 2026-11-25 the full RAG, three-agent, and clinician-approval workflow completed with Ollama for chat and embeddings. The API process's outgoing HTTP proxy was pointed at an unavailable loopback port, and a .NET HTTP probe confirmed external HTTP failed at that local proxy while loopback Ollama stayed available. However, Windows denied permission to change WLAN/firewall settings, and Wi-Fi reconnected automatically. This proves the app workflow can complete with its external HTTP path blocked; it does **not** prove the whole machine was disconnected from the Internet. Keep the T2 requirement marked partial until the disconnected run checklist below is completed. See [the recorded run](reports/offline-workflow-2026-11-25.md).

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
