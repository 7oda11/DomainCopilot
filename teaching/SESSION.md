# 90-minute teaching session: grounded healthcare RAG

## Learning outcomes

By the end, learners can: (1) explain chunking, hybrid retrieval and citations; (2) identify prompt injection as untrusted content; (3) inspect a refusal and distinguish retrieval from answer safety; (4) trace Clean Architecture dependencies; (5) explain why a clinician approval gate is mandatory; (6) run a local model fallback and describe what must be tested offline.

## Slide plan (18 slides / 90 minutes)

1. Problem and safety boundary (4 min)
2. What this MVP is and is not (4)
3. System context and personas (4)
4. Clean Architecture dependency rule (5)
5. Ingestion: extract, clean, chunk, embed (5)
6. Provenance: source/version/page/chunk (5)
7. Dense vs keyword retrieval (6)
8. Hybrid fusion and citations (6)
9. Refusal: missing evidence and dosage (6)
10. Prompt injection in user text and documents (6)
11. Researcher, Drafter, SafetyChecker (5)
12. Orchestrator and tool allow-list (5)
13. Clinician approval lifecycle (5)
14. Gemini to Ollama degraded mode (6)
15. What “offline” actually proves (5)
16. Evaluation metrics and false acceptance (5)
17. Live lab debrief / common misconceptions (5)
18. Assessment, limitations and next steps (3)

## Teaching notes

Use synthetic data only. Demonstrate one supported question, one unsupported dosage question, and a document containing an instruction-like sentence. Ask learners to trace the evidence IDs and identify which server policy blocks approval by a non-clinician. Do not present the current baseline as clinically safe: refusal agreement is weak and answer-level injection resistance needs further validation.
