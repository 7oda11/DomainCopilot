# ADR-006: bounded orchestration with an explicit clinician approval boundary

- Status: Accepted
- Date: 2026-11-24

## Context

The assessment needs specialized agents, observable progress, cancellation, safe refusal and human approval. Autonomous planning or agent-selected tools would increase risk and make the small workflow hard to audit.

## Decision

Use a bounded Application-owned orchestrator. It performs retrieval, invokes the Researcher and Drafter on fixed inputs, runs a SafetyChecker, validates citations, and may queue a draft as `AwaitingApproval`. It cannot approve. Only the API's Clinician-authorized review endpoint can persist approve/reject/edit-and-approve. Researcher and Clinician are distinct server policies. A deterministic gate blocks explicit requests to bypass safety or trigger approval before calling retrieval or a model.

## Consequences

- Fixed stages are predictable and testable; adding a stage requires an explicit application change.
- Progress can be sent over SSE and cancellation is propagated to active work.
- The gate is intentionally narrow and is not a general prompt-injection detector. Retrieved-text resistance must still be evaluated end to end.
- Human approval remains mandatory regardless of model confidence or safety output.
