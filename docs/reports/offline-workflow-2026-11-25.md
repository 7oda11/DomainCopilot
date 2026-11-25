# Offline-mode workflow verification

- Date (Africa/Cairo): 2026-11-25 06:56:13 +02:00
- Machine: RD-NB-142812
- Result: The complete local RAG, three-agent, and clinician-approval flow passed with API-process HTTP egress isolation.
- AI config: The API process was forced to Ollama for chat and embeddings. `HTTP_PROXY`, `HTTPS_PROXY`, and `ALL_PROXY` pointed to the unavailable loopback endpoint `http://127.0.0.1:1`; `NO_PROXY` allowed loopback only. A .NET `HttpClient` probe to Gemini failed with `SocketException` at that proxy, while a loopback Ollama probe succeeded.
- System-wide limitation: Wi-Fi was connected during the workflow. Windows denied WLAN/firewall configuration changes under the current user/group policy, and a Wi-Fi disconnect automatically reconnected. This proves the API ran with its outgoing HTTP path isolated and Ollama local; it does not prove the whole machine was offline.
- Local dependencies: SQL Server LocalDB readiness succeeded; LocalDB migrations were already current. Ollama models available: `qwen3:8b` and `nomic-embed-text`.
- Run ID: `0637D36B-2E41-4F39-BCF9-F84274590098`; draft ID: `FA96B062-0F61-4E14-8940-33BA32EE7E04`; status before review: `AwaitingApproval`.
- Persisted workflow agents: `GuidelineResearcher` (49,506 ms; 1,075 input / 120 output tokens), `DocumentationDrafter` (64,257 ms; 1,041 / 131), and `SafetyChecker` (54,134 ms; 1,235 / 9). Every recorded provider was `ollama:qwen3:8b`; estimated cloud cost was null.
- RAG evidence: 5 passages from the indexed synthetic corpus, including the Medication History Field Guide.
- Authorization: Researcher review returned HTTP 403. Clinician review returned `Approved`; the decision appeared in the Clinician workspace history.
- Data: all run and case content was synthetic. This report contains no credentials, JWTs, real patient data, or raw prompts.
