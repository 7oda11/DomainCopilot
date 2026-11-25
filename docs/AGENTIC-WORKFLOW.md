# Agent workflow and tool boundary

## Orchestration pattern

The workflow retrieves evidence first, then runs the Guideline Researcher and Documentation Drafter concurrently over the same evidence. The Safety Checker runs after both complete. Each model call has a 100-second agent-stage timeout, one retry with a 250 ms backoff, a provider-level timeout, and request cancellation. Provider outages degrade to local Ollama; if both providers fail, the run is marked failed and only retrieved evidence is returned.

## Agent contracts and permissions

- Guideline Researcher: receives the question and retrieved source excerpts; returns attributed findings. It cannot write data or call external systems.
- Documentation Drafter: receives researcher findings and the same excerpts; returns a draft for review. It cannot approve or release the draft.
- Safety Checker: receives the draft and source excerpts; returns a typed pass/fail decision. Invalid JSON fails closed.
- Orchestrator: controls the 3-step limit, provider selection, timeouts, run state and trace. It alone may persist a draft into `AwaitingApproval`.

Tool access is typed and allow-listed in `AgentToolCatalog`:

| Agent | Allowed tool | Effect |
|---|---|---|
| Guideline Researcher | `search-guidelines` | Read-only hybrid retrieval |
| Documentation Drafter | `validate-citations` | Read-only citation-label check |
| Safety Checker | `validate-citations` | Read-only citation-label check |
| Orchestrator | `read-clinical-case` | Read-only case lookup |
| Orchestrator | `queue-draft-for-review` | Persists a draft only in `AwaitingApproval` |

The LLMs do not receive dynamic function-calling tools; the orchestrator invokes these typed tools after model calls. The queue operation is the explicit side effect, and its only result is a pending clinician review. The human reviewer alone may approve, reject, or edit-and-approve. No EHR write or patient-care action is implemented.

## Trace and limitations

The request returns a run ID, provider/degraded-mode status, cited evidence and per-agent progress through SSE at `POST /api/copilot/drafts/stream`. Cancelling the HTTP request cancels the linked model work and marks the persisted run cancelled when the handler observes cancellation. Completed agent traces, provider token counts and optional configured cost estimates are stored in SQL and can be retrieved at `GET /api/copilot/runs/{runId}/trace`. Failed stages and retrieval are not recorded in this per-agent table yet.
