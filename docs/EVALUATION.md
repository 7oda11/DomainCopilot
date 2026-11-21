# Retrieval Evaluation

## Scope

`tests/DomainCopilot.Evaluation/evaluation-cases.json` contains 25 synthetic healthcare question/answer pairs: 16 in-corpus retrieval cases and 9 adversarial cases, including three prompt-injection probes. It contains no real patient data. Positive cases name the expected synthetic source document and the terms that should be supported by at least one returned citation.

The harness calls the same authenticated `POST /api/search` endpoint used by the application. It does not replace the API's embedding, SQL keyword search, vector search, or reciprocal-rank-fusion path with a test double.

## Metrics

- **Retrieval hit rate:** fraction of non-refusal cases where every expected source appears in the returned evidence.
- **Groundedness proxy:** fraction of labeled support claims for which all listed terms occur in at least one returned citation quote. This is a lexical support proxy, not semantic entailment or evaluation of a generated answer.
- **Refusal correctness:** fraction of cases where the API's `Refused` flag matches the dataset expectation.

The current endpoint returns retrieved evidence, not a generated answer. Therefore this harness cannot measure answer-level factual correctness or whether an agent resisted malicious instructions embedded inside an indexed document. Prompt-injection probes are counted and called out in each case report; demonstrating resistance requires a separate workflow-level adversarial evaluation after the malicious synthetic documents and corpus are available.

## Run

First validate the dataset without a database, model, or API:

```powershell
dotnet run --project tests/DomainCopilot.Evaluation -- --validate-dataset
```

For a measured run, start the application with the expected synthetic corpus indexed, then set a test identity-provider token with the role required by `/api/search`:

```powershell
$env:EVALUATION_API_URL = "http://localhost:8080"
$env:EVALUATION_BEARER_TOKEN = "<test JWT>"
$env:EVALUATION_REPORT_PATH = "evaluation-report.json"
dotnet run --project tests/DomainCopilot.Evaluation
```

The token is sent only as an Authorization header and is not included in the report or console output. Each HTTP failure stops the run with the case ID and status code; successful runs save per-case results and aggregate rates as JSON. The default report path is ignored by Git.

## Baseline status

First live retrieval baseline recorded at `2026-11-20T23:15:05Z` against the authenticated API, after indexing all 30 synthetic PDFs (150 pages; 150 chunks) into SQL Server LocalDB and the SQLite vector index. Embeddings came from local `nomic-embed-text` (768 dimensions). A temporary loopback-only test JWT issuer provided the `Clinician` token; no token or signing key is stored in this repository. The detailed per-case output is in [`reports/retrieval-baseline-2026-11-21.json`](reports/retrieval-baseline-2026-11-21.json).

| Metric | Result | Interpretation |
|---|---:|---|
| Retrieval hit rate | 100% (18/18 answerable cases) | Every expected source was returned. |
| Groundedness proxy | 100% (18/18 labeled claims) | Expected support terms appeared in cited quotes; this is lexical matching. |
| Search refusal flag agreement | 72% (18/25 cases) | Seven expected-refusal cases returned evidence and were marked not refused: H017, H018, H021, H022, H023, H024 and H025. |

The refusal metric reads the retrieval endpoint's `Refused` flag. It does not measure a generated answer or prove the agent refused unsupported requests. The current endpoint returns evidence only, so the seven misses show that retrieval returned candidates for out-of-corpus and injection queries; they do not show that an LLM gave an unsafe answer. The three prompt-injection cases remain probes only and do not verify workflow resistance.

After this retrieval run, a separate synthetic draft smoke test exercised the local Qwen chat model through the API. All three agent stages completed with five citations, and the draft reached `AwaitingApproval` with clinician approval required. The temporary JWT issuer was loopback-only. Internet access remained available during this smoke test, so it is not proof of disconnected operation.

## Known evaluation gaps

- Source names and support terms are aligned with the bundled synthetic corpus; rerun the dataset validator after editing either one.
- Groundedness checks exact support-term presence in citation text, which can miss paraphrases and can reward keyword overlap without proving entailment.
- No answer-generation route is measured, so no answer-level claim/citation entailment score exists.
- The indirect-injection case needs an indexed malicious document and a workflow-level assertion that the malicious instruction did not alter tool permissions or draft content.
- Per-request token/cost accounting and persistent agent trace metrics remain part of the broader observability work.
- The retrieval baseline measures search only; it does not include the separate local Qwen workflow smoke test described above.
