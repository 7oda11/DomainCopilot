# AI Usage Log

This file records deliberate AI-assisted engineering work for reproducibility. It does not claim that a model authored or validated the entire project. Update it when another material AI-assisted change is made.

| Date | Deliberate use | Human review / verification |
|---|---|---|
| 2026-11-20 | Drafted initial BRD and requirement traceability from the provided instructor brief | Reviewed the variant, scope and risks; stale implementation statuses are being corrected |
| 2026-11-20 | Explored Clean Architecture boundaries, repository/UoW, SQL Server and vector-store alternatives | Recorded decisions in ADRs and preserved SQL Server as canonical relational storage |
| 2026-11-21 | Designed hybrid retrieval and synthetic corpus/evaluation cases | Measured against the local corpus; results and limitations are in `docs/EVALUATION.md` |
| 2026-11-21 | Implemented provider fallback and local Ollama setup guidance | Provider fallback unit checks exist; disconnected end-to-end behavior remains unverified |
| 2026-11-24 | Reviewed role boundaries, prompt-injection risks, observability gaps and assessment deliverables | Human review identified equal-role authorization, weak refusal score and missing durable traces as blockers |

## Guardrails used

- AI-generated code and documentation are reviewed against repository behavior before being presented as complete.
- No real patient records are supplied to hosted models. Example clinical records are synthetic.
- Model-generated output is never treated as clinician approval or as clinical validation.
- Evaluation results are reported with denominators and limitations; retrieval metrics are not mislabeled as answer safety.
- Commit/PR history, test outcomes, offline runs, and human review are not fabricated.
