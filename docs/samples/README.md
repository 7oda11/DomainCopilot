# Sample retrieval documents

These PDFs are readable, fictional examples for exercising document ingestion and retrieval locally.

- `HealthCareCopilot-reference-guide.pdf` demonstrates section-based retrieval from a workflow reference.
- `HealthCareCopilot-synthetic-clinical-record-readable.pdf` demonstrates retrieval from a structured, fictional outpatient record.
- `HealthCareCopilot-synthetic-patient-HC-SYN-041.pdf` is a richer fictional outpatient record with one explicitly documented lab result and intentionally absent medication-dose/renal-function data.
- `HealthCareCopilot-two-patient-cases.json` contains ready-to-submit case summaries and questions: one asks for the documented HbA1c, and one asks for an unsupported personalized dose. See `HealthCareCopilot-two-patient-cases.md` for UI steps and expected outcomes.

All clinical records in this folder use invented identity details and measurements. They are not real patient records, clinical guidelines, or sources of care recommendations. When uploading a revised copy after indexing an older sample, use a new version number; previously indexed documents remain in the vector store until removed separately.
