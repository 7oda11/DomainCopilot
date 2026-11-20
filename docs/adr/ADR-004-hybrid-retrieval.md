# ADR-004: Reciprocal Rank Fusion for hybrid retrieval

- Status: Accepted for the assessment MVP
- Date: 2026-11-20

## Context

Healthcare questions need both semantic matching and exact term matching. Dense cosine scores and keyword scores have different scales, while citations must resolve to persisted chunks with page/version metadata.

## Decision

Run dense retrieval from the local SQLite vector index and keyword retrieval from SQL Server. Merge their ranks with Reciprocal Rank Fusion (RRF), constant `k = 60`. Resolve final IDs back through SQL Server and construct citations from the exact persisted chunk text.

## Consequences

- The vector index is derived and rebuildable; SQL Server remains canonical for source text and metadata.
- Embedding model IDs and dimensions are part of every vector lookup to prevent accidental cross-model comparisons.
- Exact cosine scan has linear cost and is appropriate for the assessment corpus only. Larger corpora need an approximate vector index and measured evaluation.
- Retrieval returns evidence or an explicit no-evidence result. It does not itself generate a clinical answer or prove that evidence is medically sufficient.
