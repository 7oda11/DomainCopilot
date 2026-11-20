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

No actual baseline is published yet. The repository does not currently ship the required 30-document / 150-page synthetic or public corpus, and a live SQL Server plus embedding provider and authenticated API were unavailable during this phase. Dataset validation is not a retrieval baseline. Record the first real run here, including zeroes and failures, only after the corpus and runtime are ready.

## Known evaluation gaps

- Source names and support terms are expected labels for the planned synthetic corpus; align them with the final corpus before treating the run as a baseline.
- Groundedness checks exact support-term presence in citation text, which can miss paraphrases and can reward keyword overlap without proving entailment.
- No answer-generation route is measured, so no answer-level claim/citation entailment score exists.
- The indirect-injection case needs an indexed malicious document and a workflow-level assertion that the malicious instruction did not alter tool permissions or draft content.
- Per-request token/cost accounting and persistent agent trace metrics remain part of the broader observability work.
